using System;
using System.Diagnostics;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;

namespace Unicorn.Data.Dilithium.Sql
{
	[DebuggerDisplay("{NameHint} ({FieldType})")]
	public class SqlItemFieldValue : IItemFieldValue
	{
		private readonly Guid _itemId;
		private readonly string _databaseName;
		private readonly string _language;
		private readonly int _version;
		private string _value;

		public SqlItemFieldValue(Guid itemId, string databaseName, string language, int version)
		{
			_itemId = itemId;
			_databaseName = databaseName;
			_language = language;
			_version = version;
		}

		public Guid FieldId { get; set; }
		public string NameHint { get; set; }

		public virtual string Value
		{
			get
			{
				if (BlobId.HasValue)
				{
					var database = Database.GetDatabase(_databaseName);

					if (!ItemManager.BlobStreamExists(BlobId.Value, database)) return string.Empty;

					var targetItem = database.GetItem(_itemId.ToString(), Language.Parse(_language), Sitecore.Data.Version.Parse(_version));

					Assert.IsNotNull(targetItem, $"The item {_itemId} {_language}#{_version} was not available in the {database.Name} database.");

					var targetField = targetItem.Fields[FieldId.ToString()];

					Assert.IsNotNull(targetField, $"Field {FieldId} was null in {database.Name}:{targetItem.Paths.FullPath} {_language}#{_version}");

					if (!targetField.HasBlobStream)
					{
						Log.Warn($"[Unicorn] The serialized blob ID {BlobId} existed in {database.Name}, but the expected item for that blob {_itemId} {_language}#{_version} did not have a blob stream according to the Sitecore API", this);
						return string.Empty;
					}

					using (var stream = targetField.GetBlobStream())
					{
						Assert.IsNotNull(stream, $"Blob stream (blob ID {BlobId.Value}) for item {_itemId} on field {FieldId} in {database.Name}:{targetItem.Paths.FullPath} was null!");

						var buf = new byte[stream.Length];

						stream.Read(buf, 0, (int)stream.Length);

						return Convert.ToBase64String(buf);
					}
				}

				return _value;
			}

			set { _value = value; }
		}

		public string FieldType { get; set; }
		public Guid? BlobId { get; set; }
	}
}
