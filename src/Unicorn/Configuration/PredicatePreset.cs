using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.XamlSharp.Xaml.Extensions;

namespace Unicorn.Configuration
{
	public class PredicatePreset
	{
		public string Id { get; set; }
		protected readonly XmlElement _configurationElement;

		public PredicatePreset(XmlElement configElement)
		{
			Assert.IsNotNull(configElement, nameof(configElement));
			var id = configElement.Attributes["id"]?.Value;
			Assert.IsNotNullOrEmpty(id, "id");
			Id = id;
			_configurationElement = configElement;
		}

		public XmlElement[] ApplyPreset(XmlElement presetInstanceElement)
		{
			Assert.IsNotNull(presetInstanceElement, nameof(presetInstanceElement));

			var replacers = new Dictionary<string,string>();

			// First we grab all the attributes of the <predicatePreset> element
			foreach (var replacer in GetReplacers(_configurationElement))
			{
				replacers[replacer.Item1] = replacer.Item2;
			}

			// Then we grab them directly from the <preset> element, allowing them to override
			foreach (var replacer in GetReplacers(presetInstanceElement))
			{
				replacers[replacer.Item1] = replacer.Item2;
			}

			// Then we grab the <predicatePreset> defined <include> entries
			var includePresets = _configurationElement.ChildNodes
				.Cast<XmlElement>()
				.Where(node => node.Name == "include")
				.ToList();

			List<XmlElement> replacedElements = new List<XmlElement>();
			foreach (var includePreset in includePresets)
			{
				replacedElements.Add(ReplaceValues(replacers, includePreset));
			}

			return replacedElements.ToArray();
		}

		// ReSharper disable once IdentifierTypo
		protected virtual XmlElement ReplaceValues(Dictionary<string, string> replacers, XmlElement includeElement)
		{
			Assert.IsNotNull(replacers, nameof(replacers));
			Assert.IsNotNull(includeElement, nameof(includeElement));

			var clone = (XmlElement) includeElement.CloneNode(true);

			bool didReplacement = false;

			// ReSharper disable once PossibleNullReferenceException
			foreach (XmlNode att in clone.Attributes)
			{
				if (att.Value.Contains("$"))
				{
					didReplacement = true;

					foreach (var replacerKey in replacers.Keys)
					{
						att.Value = Regex.Replace(att.Value, "\\$" + replacerKey, replacers[replacerKey], RegexOptions.IgnoreCase);
					}

					if (att.Value.Contains("$"))
					{
						throw new InvalidOperationException($"Predicate Preset '{((XmlElement)includeElement.ParentNode).Attributes["id"].Value}' could not be expanded, there are unresolved token attributes '{att.Value}'");
					}
				}
			}

			if (didReplacement)
			{
				return clone;
			}

			return includeElement;
		}

		protected virtual IEnumerable<Tuple<string, string>> GetReplacers(XmlElement definitionElement)
		{
			Assert.IsNotNull(definitionElement, nameof(definitionElement));
			foreach (XmlNode att in definitionElement.Attributes)
			{
				if (att.Value.Contains("$"))
				{
					throw new InvalidOperationException($"Predicate Preset attribute values cannot contain unexpanded variables. Found {att.Name}=\"{att.Value}\"");
				}

				// Note: treating these replacers as case-insensitive
				yield return new Tuple<string, string>(att.Name.ToLowerInvariant(), att.Value);
			}
		}
	}
}
