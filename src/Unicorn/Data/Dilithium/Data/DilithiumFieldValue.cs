using System;
using System.Diagnostics;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Managers;

namespace Unicorn.Data.Dilithium.Data
{
	[DebuggerDisplay("{NameHint} ({FieldType})")]
	public class DilithiumFieldValue : IItemFieldValue
	{
		private readonly Guid _itemId;
		private readonly string _databaseName;
		private string _value;

		public DilithiumFieldValue(Guid itemId, string databaseName)
		{
			_itemId = itemId;
			_databaseName = databaseName;
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

					using (var stream = database.GetItem(_itemId.ToString()).Fields[FieldId.ToString()].GetBlobStream())
					{
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
