using System;
using System.Collections.Generic;
using System.IO;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.StringExtensions;
using Unicorn.ControlPanel;
using Unicorn.Data;

namespace Unicorn.Serialization.Sitecore.Formatting
{
	public class SitecoreSerializationFormatter : ISitecoreSerializationFormatter, IDocumentable
	{
		public virtual void Serialize(SyncItem item, Stream outputStream)
		{
			using (var writer = new StreamWriter(outputStream))
			{
				item.Serialize(writer);
			}
		}

		public virtual ISourceItem Deserialize(ISerializedItem serializedItem, bool ignoreMissingTemplateFields)
		{
			if(ignoreMissingTemplateFields) throw new NotSupportedException("The Sitecore serialization engine does not support ignoring missing fields.");

			try
			{
				var options = new LoadOptions { DisableEvents = true, ForceUpdate = true, UseNewID = false };

				return new SitecoreSourceItem(ItemSynchronization.PasteSyncItem(((SitecoreSerializedItem)serializedItem).InnerItem, options, true));
			}
			catch (ParentItemNotFoundException ex)
			{
				string error = "Cannot load item from path '{0}'. Probable reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex.ParentID);

				throw new DeserializationException(error, ex);
			}
			catch (ParentForMovedItemNotFoundException ex2)
			{
				string error = "Item from path '{0}' cannot be moved to appropriate location. Possible reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex2.ParentID);

				throw new DeserializationException(error, ex2);
			}
		}

		public SyncItem Read(Stream sourceStream)
		{
			using (var reader = new StreamReader(sourceStream))
			{
				return SyncItem.ReadItem(new Tokenizer(reader), true);
			}
		}

		public virtual string FriendlyName
		{
			get { return "Sitecore Serialization Formatter"; }
		}

		public virtual string Description
		{
			get { return "Serializes items using the standard Sitecore serialization format and APIs (.item files)"; }
		}

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return null;
		}
	}
}
