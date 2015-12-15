using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace Unicorn.Data.DataProvider
{
	/// <summary>
	/// Evaluates a Sitecore FieldChanges and applies it to a set of Unicorn IItemFieldValues.
	/// Changes from the FieldChanges override any existing values in the Unicorn values. Values without changes
	/// are preserved.
	/// </summary>
	/// <remarks>
	/// This is necessary because when installing packages and update packages Sitecore can provide only PARTIAL item data
	/// to the data provider (e.g. changes.Item is INCOMPLETE). Parsing and applying the changes to the existing item, if any,
	/// enables us to augment the item with the changes from Sitecore without tossing out any incomplete data the save may include.
	/// </remarks>
	public class FieldChangeParser
	{
		public IEnumerable<IItemFieldValue> ParseFieldChanges(IEnumerable<FieldChange> applicableFieldChanges, IEnumerable<IItemFieldValue> baseFieldValues)
		{
			var fields = baseFieldValues?.ToDictionary(value => value.FieldId) ?? new Dictionary<Guid, IItemFieldValue>();

			foreach (var change in applicableFieldChanges)
			{
				if (change.RemoveField)
				{
					fields.Remove(change.FieldID.Guid);
					continue;
				}

				IItemFieldValue targetField;
				ProxyFieldValue newChangeValue;

				if(fields.TryGetValue(change.FieldID.Guid, out targetField)) newChangeValue = new ProxyFieldValue(targetField);
				else newChangeValue = new ProxyFieldValue(change.FieldID.Guid, change.Value);

				newChangeValue.FieldType = change.Definition.Type;
				newChangeValue.NameHint = change.Definition.Name;
				newChangeValue.Value = change.Value;

				fields[change.FieldID.Guid] = newChangeValue;
			}

			return fields.Values;
		}
	}
}
