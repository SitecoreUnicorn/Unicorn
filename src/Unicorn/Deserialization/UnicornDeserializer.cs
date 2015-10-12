using Rainbow.Filtering;
using Rainbow.Storage.Sc.Deserialization;
using Sitecore.Data;
using Sitecore.Data.Templates;

namespace Unicorn.Deserialization
{
	public class UnicornDeserializer : DefaultDeserializer
	{
		public UnicornDeserializer(IDefaultDeserializerLogger logger, IFieldFilter fieldFilter) : base(logger, fieldFilter)
		{
		}

		protected override Template AssertTemplate(Database database, ID templateId, string itemPath)
		{
			// This fixes the case where we have TpSync'd templates.
			// In that situation, we need to explicitly ENABLE serialization and TpSync
			// while asserting the template or else we would be unable to sync any items
			// that were using a template that was only in TpSync.
			using (new TransparentSyncEnabler())
			{
				using (new SerializationEnabler())
				{
					return base.AssertTemplate(database, templateId, itemPath);
				}
			}
		}
	}
}
