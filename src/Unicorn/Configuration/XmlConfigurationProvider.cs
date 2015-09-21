using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using Rainbow.SourceControl;
using Rainbow.Storage;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.Configuration
{
	/// <summary>
	/// Reads Unicorn dependency configurations from XML (e.g. Sitecore web.config section sitecore/unicorn)
	/// </summary>
	public class XmlConfigurationProvider : IConfigurationProvider
	{
		private IConfiguration[] _configurations;

		public IConfiguration[] Configurations
		{
			get
			{
				if (_configurations == null) LoadConfigurations();
				return _configurations;
			}
		}

		protected virtual XmlNode GetConfigurationNode()
		{
			return Factory.GetConfigNode("/sitecore/unicorn");
		}

		protected virtual void LoadConfigurations()
		{
			var configNode = GetConfigurationNode();

			Assert.IsNotNull(configNode, "Root Unicorn config node not found. Missing Unicorn.config?");

			var defaultsNode = configNode["defaults"];

			Assert.IsNotNull(defaultsNode, "Unicorn <defaults> node not found. It should be under <unicorn> config section.");

			var configurationNodes = configNode.SelectNodes("./configurations/configuration");

			// no configs let's get outta here
			if (configurationNodes == null || configurationNodes.Count == 0)
			{
				_configurations = new IConfiguration[0];
				return;
			}

			var configurations = new Collection<IConfiguration>();
			var nameChecker = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (XmlElement element in configurationNodes)
			{
				var configuration = LoadConfiguration(element, defaultsNode);

				if (nameChecker.Contains(configuration.Name)) throw new InvalidOperationException("The Unicorn configuration '" + configuration.Name + "' is defined twice. Configurations should have unique names.");
				nameChecker.Add(configuration.Name);

				configurations.Add(configuration);
			}

			_configurations = configurations.ToArray();
		}

		protected virtual IConfiguration LoadConfiguration(XmlElement configuration, XmlElement defaults)
		{
			var name = GetAttributeValue(configuration, "name");

			Assert.IsNotNullOrEmpty(name, "Configuration node had empty or missing name attribute.");

			var description = GetAttributeValue(configuration, "description");

			var registry = new MicroConfiguration(name, description);

			// these are config types we absolutely must have instances of to use Unicorn - an exception will throw if they don't exist
			var configMapping = new Dictionary<string, Action<XmlElement, XmlElement, string, IConfiguration>>
			{
				{"sourceDataStore", RegisterExpectedConfigType<ISourceDataStore>},
				{"evaluator", RegisterExpectedConfigType<IEvaluator>},
				{"predicate", RegisterExpectedConfigType<IPredicate>},
				{"targetDataStore", RegisterExpectedConfigType<ITargetDataStore>},
				{"sourceControlManager", RegisterExpectedConfigType<ISourceControlManager>},
				{"sourceControlSync", RegisterExpectedConfigType<ISourceControlSync>},
				{"logger", RegisterExpectedConfigType<ILogger>},
				{"loaderLogger", RegisterExpectedConfigType<ISerializationLoaderLogger>},
				{"loaderConsistencyChecker", RegisterExpectedConfigType<IConsistencyChecker>},
				{"loaderDeserializeFailureRetryer", RegisterExpectedConfigType<IDeserializeFailureRetryer>}
			};

			foreach (var explicitMapping in configMapping)
			{
				explicitMapping.Value(configuration, defaults, explicitMapping.Key, registry);
			}

			// now we get a list of any nodes that are NOT on the explicit map list.
			// these nodes are ad-hoc DI registrations where we'll register any interfaces they implement with the container
			// for example loggers and other non-essential mappings get loaded here. As usual, specific config overrides defaults.
			var configurationAdHocRegisterNodes = configuration.ChildNodes.OfType<XmlElement>().Where(x => !configMapping.ContainsKey(x.Name)).ToArray();
			// ReSharper disable once SimplifyLinqExpression
			var defaultAdHocRegisterNodes = defaults.ChildNodes.OfType<XmlElement>().Where(node => !configMapping.ContainsKey(node.Name) && !configurationAdHocRegisterNodes.Any(x => x.Name == node.Name)).ToArray();
			// note that the default nodes remove dupes from the local configuration in the statement above.

			foreach (XmlElement adHocElement in configurationAdHocRegisterNodes.Concat(defaultAdHocRegisterNodes))
			{
				RegisterGenericConfigTypeByInterfaces(configuration, defaults, adHocElement.Name, registry);
			}

			return new ReadOnlyConfiguration(registry);
		}

		/// <summary>
		/// Registers an ad-hoc DI entry with the configuration, using its interfaces as the registrations
		/// </summary>
		protected virtual void RegisterGenericConfigTypeByInterfaces(XmlElement configuration, XmlElement defaults, string elementName, IConfiguration registry)
		{
			var type = GetConfigType(configuration, defaults, elementName);

			var interfaces = type.Type.GetInterfaces();

			var attributes = GetUnmappedAttributes(configuration, defaults, elementName);

			foreach (var @interface in interfaces)
			{
				registry.Register(@interface, () => registry.Activate(type.Type, attributes), type.SingleInstance);
			}
		}

		/// <summary>
		/// Registers an expected DI entry with the configuration. If the type is incorrect, or does not exist, an exception is thrown.
		/// </summary>
		protected virtual void RegisterExpectedConfigType<TResultType>(XmlElement configuration, XmlElement defaults, string elementName, IConfiguration registry)
			where TResultType : class
		{
			var type = GetConfigType(configuration, defaults, elementName);
			var attributes = GetUnmappedAttributes(configuration, defaults, elementName);
			var resultType = typeof(TResultType);

			if (resultType == typeof(ISourceDataStore))
			{
				Func<IDataStore> factory = () => (IDataStore)registry.Activate(type.Type, attributes);
				Func<object> wrapperFactory = () => new ConfigurationDataStore(new Lazy<IDataStore>(factory));

				registry.Register(resultType, wrapperFactory, type.SingleInstance);
				return;
			}

			if (resultType == typeof(ITargetDataStore))
			{
				Func<IDataStore> factory = () => (IDataStore)registry.Activate(type.Type, attributes);
				Func<object> wrapperFactory = () => new ConfigurationDataStore(new Lazy<IDataStore>(factory));

				registry.Register(resultType, wrapperFactory, type.SingleInstance);
				return;
			}

			if (!resultType.IsAssignableFrom(type.Type))
				throw new InvalidOperationException("Invalid type for Unicorn config node {0} (expected {1} implementation)".FormatWith(elementName, typeof(TResultType).FullName));

			RegisterGenericConfigTypeByInterfaces(configuration, defaults, elementName, registry);
		}

		/// <summary>
		/// Resolves an attribute of an XML element into a C# Type, using the Assembly Qualified Name
		/// </summary>
		protected virtual TypeRegistration GetConfigType(XmlElement configuration, XmlElement defaults, string elementName)
		{
			var typeNode = configuration[elementName] ?? defaults[elementName];

			Assert.IsNotNull(typeNode, "Could not find a valid value for Unicorn config node " + elementName);

			var typeString = GetAttributeValue(typeNode, "type");

			var isSingleInstance = "true".Equals(GetAttributeValue(typeNode, "singleInstance"));

			Assert.IsNotNullOrEmpty(typeString, "Missing value for Unicorn config node {0}, type attribute (type expected).".FormatWith(elementName));

			var type = Type.GetType(typeString, false);

			Assert.IsNotNull(type, "Invalid type {0} for Unicorn config node {1}, attribute type".FormatWith(typeString, elementName));

			return new TypeRegistration { Type = type, SingleInstance = isSingleInstance };
		}

		/// <summary>
		/// Gets unmapped (i.e. not 'type') attributes or body of a dependency declaration. These are passed as possible constructor parameters to the object.
		/// </summary>
		protected virtual KeyValuePair<string, object>[] GetUnmappedAttributes(XmlElement configuration, XmlElement defaults, string elementName)
		{
			var typeNode = configuration[elementName] ?? defaults[elementName];

			Assert.IsNotNull(typeNode, "Could not find a valid value for Unicorn config node " + elementName);

			// ReSharper disable once PossibleNullReferenceException
			var attributes = typeNode.Attributes.Cast<XmlAttribute>()
				.Where(attr => attr.Name != "type" && attr.Name != "singleInstance")
				.Select(attr =>
				{
					bool boolean;
					if (bool.TryParse(attr.InnerText, out boolean)) return new KeyValuePair<string, object>(attr.Name, boolean);

					var value = attr.InnerText.Replace("$(configurationName)", GetAttributeValue(configuration, "name"));

					return new KeyValuePair<string, object>(attr.Name, value);
				});

			// we pass it the XML element as 'configNode'
			attributes = attributes.Concat(new[] { new KeyValuePair<string, object>("configNode", typeNode) });

			return attributes.ToArray();
		}

		/// <summary>
		/// Gets an XML attribute value, returning null if it does not exist and its inner text otherwise.
		/// </summary>
		protected virtual string GetAttributeValue(XmlNode node, string attribute)
		{
			if (node == null || node.Attributes == null) return null;

			var attributeItem = node.Attributes[attribute];

			if (attributeItem == null) return null;

			return attributeItem.InnerText;
		}

		protected class TypeRegistration
		{
			public Type Type { get; set; }
			public bool SingleInstance { get; set; }
		}
	}
}
