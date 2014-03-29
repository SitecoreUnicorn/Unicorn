using System;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Predicates;

namespace Unicorn.Serialization.Sitecore.Fiat
{
	public class FiatSitecoreSerializationProvider : SitecoreSerializationProvider
	{
		public override string FriendlyName
		{
			get { return "Fiat Sitecore Serialization Provider"; }
		}

		public override string Description
		{
			get { return "Stores serialized items using Sitecore's built-in serialization engine. Uses a custom deserializer that allows much more information to be gleaned compared to the default APIs, is faster, and supports field exclusions."; }
		}

		private readonly FiatDeserializer _deserializer;
		public FiatSitecoreSerializationProvider(IPredicate predicate, IFieldPredicate fieldPredicate, IFiatDeserializerLogger logger, string rootPath = null, string logName = "UnicornItemSerialization")
			: base(predicate, rootPath, logName)
		{
			Assert.ArgumentNotNull(logger, "logger");

			_deserializer = new FiatDeserializer(logger, fieldPredicate);
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
