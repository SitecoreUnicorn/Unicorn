using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Predicates.Exclusions;

namespace Unicorn.Predicates
{
	public class SerializationPresetPredicate : IPredicate, ITreeRootFactory
	{
		private readonly IList<PresetTreeRoot> _includeEntries;

		public SerializationPresetPredicate(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			_includeEntries = ParsePreset(configNode);
		}

		public PredicateResult Includes(IItemData itemData)
		{
			Assert.ArgumentNotNull(itemData, "itemData");

			// no entries = include everything
			if (_includeEntries.Count == 0) return new PredicateResult(true);

			var result = new PredicateResult(true);

			PredicateResult priorityResult = null;

			foreach (var entry in _includeEntries)
			{
				result = Includes(entry, itemData);

				if (result.IsIncluded) return result; // it's definitely included if anything includes it
				if (!string.IsNullOrEmpty(result.Justification)) priorityResult = result; // a justification means this is probably a more 'important' fail than others
			}

			return priorityResult ?? result; // return the last failure
		}

		public TreeRoot[] GetRootPaths()
		{
			return _includeEntries.ToArray<TreeRoot>();
		}

		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		protected PredicateResult Includes(PresetTreeRoot entry, IItemData itemData)
		{
			// check for db match
			if (!itemData.DatabaseName.Equals(entry.DatabaseName, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check for path match
			if (!itemData.Path.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check excludes
			return ExcludeMatches(entry, itemData);
		}

		protected virtual PredicateResult ExcludeMatches(PresetTreeRoot entry, IItemData itemData)
		{
			foreach (var exclude in entry.Exclusions)
			{
				var result = exclude.Evaluate(itemData.Path);

				if (!result.IsIncluded) return result;
			}
			
			return new PredicateResult(true);
		}

		[ExcludeFromCodeCoverage]
		public string FriendlyName => "Serialization Preset Predicate";

		[ExcludeFromCodeCoverage]
		public string Description => "Defines what to include in Unicorn based on .";

		[ExcludeFromCodeCoverage]
		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			var configs = new Collection<KeyValuePair<string, string>>();
			foreach (var entry in _includeEntries)
			{
				string basePath = entry.DatabaseName + ":" + entry.Path;
				string excludes = GetExcludeDescription(entry);

				configs.Add(new KeyValuePair<string, string>(entry.Name, basePath + excludes));
			}

			return configs.ToArray();
		}

		[ExcludeFromCodeCoverage]
		private string GetExcludeDescription(PresetTreeRoot entry)
		{
			if (entry.Exclusions.Count == 0) return string.Empty;

			// ReSharper disable once UseStringInterpolation
			return string.Format(" (except {0})", string.Join(", ", entry.Exclusions.Select(exclude => exclude.Description)));
		}

		private IList<PresetTreeRoot> ParsePreset(XmlNode configuration)
		{
			var presets = configuration.ChildNodes
				.Cast<XmlNode>()
				.Where(node => node.Name == "include")
				.Select(CreateIncludeEntry)
				.ToList();

			var names = new HashSet<string>();
			foreach (var preset in presets)
			{
				if (!names.Contains(preset.Name))
				{
					names.Add(preset.Name);
					continue;
				}

				throw new InvalidOperationException("Multiple predicate include nodes had the same name '{0}'. This is not allowed. Note that this can occur if you did not specify the name attribute and two include entries end in an item with the same name. Use the name attribute on the include tag to give a unique name.".FormatWith(preset.Name));
			}

			return presets;
		}

		protected virtual PresetTreeRoot CreateIncludeEntry(XmlNode configuration)
		{
			string database = GetExpectedAttribute(configuration, "database");
			string path = GetExpectedAttribute(configuration, "path");

			// ReSharper disable once PossibleNullReferenceException
			var name = configuration.Attributes["name"];
			string nameValue = name == null ? path.Substring(path.LastIndexOf('/') + 1) : name.Value;

			var root = new PresetTreeRoot(nameValue, path, database);

			root.Exclusions = configuration.ChildNodes
				.OfType<XmlElement>()
				.Where(element => element.Name.Equals("exclude"))
				.Select(excludeNode => CreateExcludeEntry(excludeNode, root))
				.ToList();

			return root;
		}

		protected virtual IPresetTreeExclusion CreateExcludeEntry(XmlElement excludeNode, PresetTreeRoot root)
		{
			if (excludeNode.HasAttribute("path"))
			{
				return new PathBasedPresetTreeExclusion(GetExpectedAttribute(excludeNode, "path"), root);
			}

			var exclusions = excludeNode.ChildNodes
				.OfType<XmlElement>()
				.Where(element => element.Name.Equals("except") && element.HasAttribute("name"))
				.Select(element => GetExpectedAttribute(element, "name"))
				.ToArray();

			if (excludeNode.HasAttribute("children"))
			{
				return new ChildrenOfPathBasedPresetTreeExclusion(root.Path, exclusions, root);
			}

			if (excludeNode.HasAttribute("childrenOfPath"))
			{
				return new ChildrenOfPathBasedPresetTreeExclusion(GetExpectedAttribute(excludeNode, "childrenOfPath"), exclusions, root);
			}

			throw new InvalidOperationException($"Unable to parse invalid exclusion value: {excludeNode.InnerXml}");
		}

		private static string GetExpectedAttribute(XmlNode node, string attributeName)
		{
			// ReSharper disable once PossibleNullReferenceException
			var attribute = node.Attributes[attributeName];

			if (attribute == null) throw new InvalidOperationException("Missing expected '{0}' attribute on '{1}' node while processing predicate: {2}".FormatWith(attributeName, node.Name, node.OuterXml));

			return attribute.Value;
		}

		IEnumerable<TreeRoot> ITreeRootFactory.CreateTreeRoots()
		{
			return GetRootPaths();
		}
	}
}