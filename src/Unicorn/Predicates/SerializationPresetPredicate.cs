using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Serialization;

namespace Unicorn.Predicates
{
	public class SerializationPresetPredicate : IPredicate
	{
		private readonly IList<IncludeEntry> _preset;

		public SerializationPresetPredicate() : this("default")
		{
			
		}

		public SerializationPresetPredicate(string presetName)
		{
			Assert.ArgumentNotNullOrEmpty(presetName, "presetName");

			var config = Factory.GetConfigNode("serialization/" + presetName);
			
			if (config == null)
				throw new InvalidOperationException("Preset " + presetName + " is undefined in configuration.");
		}

		public SerializationPresetPredicate(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, "configNode");

			_preset = PresetFactory.Create(configNode);
		}

		public string Name { get { return "Serialization Preset"; } }

		public PredicateResult Includes(ISourceItem item)
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

		public PredicateResult Includes(ISerializedReference item)
		{
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

		public void SerializeAll(ISerializationProvider provider)
		{
			// TODO
		}

		// TODO: this may be used as part of SerializeAll but otherwise is vestigal
		private Item[] GetRootItems()
		{
			var items = new List<Item>();

			foreach (var include in _preset)
			{
				var item = Factory.GetDatabase(include.Database).GetItem(include.Path);

				if (item != null) items.Add(item);
				else Log.Warn("Unable to resolve root item for serialization preset {0}:{1}".FormatWith(include.Database, include.Path), this);
			}

			return items.ToArray();
		}

		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		protected PredicateResult Includes(IncludeEntry entry, ISourceItem item)
		{
			// check for db match
			if (item.DatabaseName != entry.Database) return new PredicateResult(false);

			// check for path match
			if (!item.ItemPath.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check excludes
			return ExcludeMatches(entry, item);
		}

		/// <summary>
		/// Checks if a preset includes a given serialized item
		/// </summary>
		protected PredicateResult Includes(IncludeEntry entry, ISerializedReference item)
		{
			// check for db match
			if (item.DatabaseName != entry.Database) return new PredicateResult(false);

			// check for path match
			if (!item.ItemPath.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// check excludes
			return ExcludeMatches(entry, item);
		}

		protected virtual PredicateResult ExcludeMatches(IncludeEntry entry, ISourceItem item)
		{
			PredicateResult result = ExcludeMatchesTemplate(entry.Exclude, item.TemplateName);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesTemplateId(entry.Exclude, item.TemplateId);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesPath(entry.Exclude, item.ItemPath);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesId(entry.Exclude, item.Id);

			return result;
		}

		protected virtual PredicateResult ExcludeMatches(IncludeEntry entry, ISerializedReference reference)
		{
			PredicateResult result = ExcludeMatchesPath(entry.Exclude, reference.ItemPath);

			if (!result.IsIncluded) return result;

			// many times the ISerializedReference may also have an item ref in it (e.g. be a serialized item)
			// in this case we can check additional criteria
			var item = reference as ISerializedItem;

			if (item == null) return result;

			result = ExcludeMatchesTemplateId(entry.Exclude, ID.Parse(item.TemplateId));

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesTemplate(entry.Exclude, item.TemplateName);

			if (!result.IsIncluded) return result;

			result = ExcludeMatchesId(entry.Exclude, ID.Parse(item.Id));

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
		protected virtual PredicateResult ExcludeMatchesId(IEnumerable<ExcludeEntry> entries, ID id)
		{
			bool match = entries.Any(entry => entry.Type.Equals("id", StringComparison.Ordinal) && entry.Value.Equals(id.ToString(), StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item ID exclusion rule")
						: new PredicateResult(true);
		}

		/// <summary>
		/// Checks if a given list of excludes matches a specific template name
		/// </summary>
		protected virtual PredicateResult ExcludeMatchesTemplate(IEnumerable<ExcludeEntry> entries, string templateName)
		{
			bool match = entries.Any(entry => entry.Type.Equals("template", StringComparison.Ordinal) && entry.Value.Equals(templateName, StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item template name exclusion rule")
						: new PredicateResult(true);
		}

		/// <summary>
		/// Checks if a given list of excludes matches a specific template ID
		/// </summary>
		protected virtual PredicateResult ExcludeMatchesTemplateId(IEnumerable<ExcludeEntry> entries, ID templateId)
		{
			bool match = entries.Any(entry => entry.Type.Equals("templateid", StringComparison.Ordinal) && entry.Value.Equals(templateId.ToString(), StringComparison.OrdinalIgnoreCase));

			return match
						? new PredicateResult("Item template ID exclusion rule")
						: new PredicateResult(true);
		}
	}
}
