using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands.Serialization;
using Unicorn.Data;
using Unicorn.Predicates;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.UI.Commands
{
	public class UnicornLoadItemCommand : LoadItemCommand
	{
		private readonly SerializationHelper _helper;

		public UnicornLoadItemCommand() : this(new SerializationHelper())
		{

		}

		public UnicornLoadItemCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override Item LoadItem(Item item, LoadOptions options)
		{
			Assert.ArgumentNotNull(item, "item");

			var itemData = new ItemData(item);

			var configuration = _helper.GetConfigurationsForItem(itemData).FirstOrDefault(); // if multiple configs contain item, load from first one

			if (configuration == null) return base.LoadItem(item, options);

			var sourceStore = configuration.Resolve<ISourceDataStore>();
			var targetStore = configuration.Resolve<ITargetDataStore>();

			var targetItem = targetStore.GetByPathAndId(itemData.Path, itemData.Id, itemData.DatabaseName);

			if (targetItem == null)
			{
				Log.Warn("Unicorn: Unable to load item because it was not serialized.", this);
				return base.LoadItem(item, options);
			}

			var result = configuration.Resolve<IPredicate>().Includes(targetItem);
			sourceStore.Save(targetItem, result.FieldValueManipulator);

			return Database.GetItem(item.Uri);
		}
	}
}
