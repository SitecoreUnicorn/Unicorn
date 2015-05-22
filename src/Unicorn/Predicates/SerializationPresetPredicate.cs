using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using Gibson.Model;
using Gibson.Predicates;
using Sitecore.Data;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;
using Unicorn.ControlPanel;

namespace Unicorn.Predicates
{
	public class SerializationPresetPredicate : IPredicate, IDocumentable
	{
		private readonly IList<IncludeEntry> _preset;

		public SerializationPresetPredicate(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			_preset = PresetFactory.Create(configNode);
		}

		public string Name { get { return "Serialization Preset"; } }

		public PredicateResult Includes(ISerializableItem item)
		{
			// no entries = include everything
			if (_preset.FirstOrDefault() == null) return new PredicateResult(true);

			var result = new PredicateResult(true);
			PredicateResult priorityResult = null;
			foreach (var entry in _preset)
			{
				result = Includes(entry, item);

				if (result.IsIncluded) return result; // it's definitely included if anything includes it
				if (!string.IsNullOrEmpty(result.Justification)) priorityResult = result; // a justification means this is probably a more 'important' fail than others
			}

			return priorityResult ?? result; // return the last failure
		}

		public PredicateRootPath[] GetRootPaths()
		{
			return _preset.Select(include => new PredicateRootPath(include.Database, include.Path)).ToArray();
		}



		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		protected PredicateResult Includes(IncludeEntry entry, ISerializableItem item)
		{
			// check for db match
			if (item.DatabaseName != entry.Database) return new PredicateResult(false);

			// check for path match
			if (!item.Path.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check excludes
			return ExcludeMatches(entry, item);
		}

		protected virtual PredicateResult ExcludeMatches(IncludeEntry entry, ISerializableItem item)
		{
			PredicateResult result = ExcludeMatchesTemplateId(entry.Exclude, item.TemplateId);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesPath(entry.Exclude, item.Path);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesId(entry.Exclude, item.Id);

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

		/// <summary>
		/// Checks if a given list of excludes matches a specific item ID. Use ID.ToString() format eg {A9F4...}
		/// </summary>
		protected virtual PredicateResult ExcludeMatchesId(IEnumerable<ExcludeEntry> entries, Guid id)
		{
			bool match = entries.Any(entry => entry.Type.Equals("id", StringComparison.Ordinal) && entry.Value.Equals(new ID(id).ToString(), StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item ID exclusion rule")
						: new PredicateResult(true);
		}

		/// <summary>
		/// Checks if a given list of excludes matches a specific template ID
		/// </summary>
		protected virtual PredicateResult ExcludeMatchesTemplateId(IEnumerable<ExcludeEntry> entries, Guid templateId)
		{
			bool match = entries.Any(entry => entry.Type.Equals("templateid", StringComparison.Ordinal) && entry.Value.Equals(new ID(templateId).ToString(), StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item template ID exclusion rule")
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
				string basePath = entry.Database + ":" + entry.Path;
				string excludes = GetExcludeDescription(entry);

				configs.Add(new KeyValuePair<string, string>("Included path", basePath + excludes));
			}

			return configs.ToArray();
		}

		private string GetExcludeDescription(IncludeEntry entry)
		{
			if (entry.Exclude.Count == 0) return string.Empty;

			return string.Format(" (except {0})", string.Join(", ", entry.Exclude.Select(x => x.Type + ":" + x.Value)));
		}
	}
}