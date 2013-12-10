using System;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Dependencies;
using Unicorn.Predicates;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class FiatSitecoreSerializationProvider : SitecoreSerializationProvider
	{
		private readonly FiatDeserializer _deserializer;
		public FiatSitecoreSerializationProvider(string rootPath = null, string logName = "UnicornItemSerialization", IPredicate predicate = null, IFiatDeserializerLogger logger = null) : base(rootPath, logName, predicate)
		{
			logger = logger ?? Registry.Current.Resolve<IFiatDeserializerLogger>();

			_deserializer = new FiatDeserializer(logger);
		}

		public override ISourceItem DeserializeItem(ISerializedItem serializedItem, bool ignoreMissingTemplateFields)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			try
			{
				return new SitecoreSourceItem(_deserializer.PasteSyncItem(typed.InnerItem, ignoreMissingTemplateFields));
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
	}
}
