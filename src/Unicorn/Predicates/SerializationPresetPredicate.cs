using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using Rainbow.Model;
using Rainbow.Storage;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Unicorn.Predicates
{
	public class SerializationPresetPredicate : IPredicate, ITreeRootFactory
	{
		private readonly IList<PresetTreeRoot> _preset;

		public SerializationPresetPredicate(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			_preset = ParsePreset(configNode);
		}

		public PredicateResult Includes(IItemData itemData)
		{
			Assert.ArgumentNotNull(itemData, "itemData");

			// no entries = include everything
			if (_preset.FirstOrDefault() == null) return new PredicateResult(true);

			var result = new PredicateResult(true);
			PredicateResult priorityResult = null;
			foreach (var entry in _preset)
			{
				result = Includes(entry, itemData);

				if (result.IsIncluded) return result; // it's definitely included if anything includes it
				if (!string.IsNullOrEmpty(result.Justification)) priorityResult = result; // a justification means this is probably a more 'important' fail than others
			}

			return priorityResult ?? result; // return the last failure
		}

		public TreeRoot[] GetRootPaths()
		{
			return _preset.ToArray<TreeRoot>();
		}

		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		protected PredicateResult Includes(PresetTreeRoot entry, IItemData itemData)
		{
			// check for db match
			if (itemData.DatabaseName != entry.DatabaseName) return new PredicateResult(false);

			// check for path match
			if (!itemData.Path.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check excludes
			return ExcludeMatches(entry, itemData);
		}

		protected virtual PredicateResult ExcludeMatches(PresetTreeRoot entry, IItemData itemData)
		{
			PredicateResult result = ExcludeMatchesPath(entry.Exclude, itemData.Path);

			if (!result.IsIncluded) return result;

			return result;
		}

		/// <summary>
		/// Checks if a given list of excludes matches a specific Serialization path
		/// </summary>
		protected virtual PredicateResult ExcludeMatchesPath(IEnumerable<ExcludeEntry> entries, string sitecorePath)
		{
			bool match = entries.Any(entry => entry.Type.Equals("path", StringComparison.Ordinal) && sitecorePath.StartsWith(entry.Value, StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item path exclusion rule")
						: new PredicateResult(true);
		}

		public string FriendlyName
		{
			get { return "Serialization Preset Predicate"; }
		}

		public string Description
		{
			get { return "Defines what to include in Unicorn based on Sitecore's built in serialization preset system (documented in the Serialization Guide). This is the default predicate."; }
		}

		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			var configs = new Collection<KeyValuePair<string, string>>();
			foreach (var entry in _preset)
			{
				string basePath = entry.DatabaseName + ":" + entry.Path;
				string excludes = GetExcludeDescription(entry);

				configs.Add(new KeyValuePair<string, string>(entry.Name, basePath + excludes));
			}

			return configs.ToArray();
		}

		private string GetExcludeDescription(PresetTreeRoot entry)
		{
			if (entry.Exclude.Count == 0) return string.Empty;

			return string.Format(" (except {0})", string.Join(", ", entry.Exclude.Select(x => x.Type + ":" + x.Value)));
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

		private static PresetTreeRoot CreateIncludeEntry(XmlNode configuration)
		{
			var name = configuration.Attributes["name"];
			string database = GetExpectedAttribute(configuration, "database");
			string path = GetExpectedAttribute(configuration, "path");
			var exclude = configuration.ChildNodes
				.OfType<XmlElement>()
				.Where(node => node.Name == "exclude")
				.Select(CreateExcludeEntry)
				.ToList();

			string nameValue = name == null ? path.Substring(path.LastIndexOf('/') + 1) : name.Value;

			return new PresetTreeRoot(nameValue, path, database, exclude);
		}

		private static ExcludeEntry CreateExcludeEntry(XmlNode configuration)
		{
			ExcludeEntry excludeEntry = new ExcludeEntry();
			excludeEntry.Type = configuration.Attributes[0].Name;
			excludeEntry.Value = configuration.Attributes[0].Value;
			return excludeEntry;
		}

		private static string GetExpectedAttribute(XmlNode node, string attributeName)
		{
			var attribute = node.Attributes[attributeName];

			if(attribute == null) throw new InvalidOperationException("Missing expected '{0}' attribute on '{1}' node while processing predicate: {2}".FormatWith(attributeName, node.Name, node.OuterXml));

			return attribute.Value;
		}

		IEnumerable<TreeRoot> ITreeRootFactory.CreateTreeRoots()
		{
			return GetRootPaths();
		}
	}
}