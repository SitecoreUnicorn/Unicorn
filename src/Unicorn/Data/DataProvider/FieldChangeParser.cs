using System;
using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;

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
		public IEnumerable<IItemFieldValue> ParseFieldChanges(IEnumerable<FieldChange> applicableFieldChanges, IEnumerable<IItemFieldValue> baseFieldValues, string databaseName)
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

				if (!change.IsBlob)
				{
					newChangeValue.Value = change.Value;
				}
				else
				{
					Guid blobId;
					if (Guid.TryParse(change.Value, out blobId))
					{
						newChangeValue.BlobId = blobId;
						newChangeValue.Value = GetBlobValue(blobId, databaseName);
					}
					else
					{
						newChangeValue.BlobId = null;
						newChangeValue.Value = null;
					}
				}

				fields[change.FieldID.Guid] = newChangeValue;
			}

			return fields.Values;
		}

		protected string GetBlobValue(Guid blobId, string database)
		{
			var db = Factory.GetDatabase(database);

			Assert.IsNotNull(db, "Database {0} does not exist!", database);

			using (var stream = ItemManager.GetBlobStream(blobId, db))
			{
				// during package installs the blob might not yet be in the database.
				// so if it does not yet exist we'll act like the value is blank.
				// this works out because the blob is actually written in a later data push from the package installer
				// so the blob actually does end up in the item
				if (stream == null) return string.Empty;

				var buf = new byte[stream.Length];

				stream.Read(buf, 0, (int)stream.Length);

				return Convert.ToBase64String(buf);
			}
		}
	}
}
