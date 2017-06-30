using System;
using System.Linq;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Shell.Framework.Commands.Serialization;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncEnd;
using ItemData = Rainbow.Storage.Sc.ItemData;

namespace Unicorn.UI.Commands
{
	public class UnicornLoadTreeCommand : LoadTreeCommand
	{
		private readonly SerializationHelper _helper;

		public UnicornLoadTreeCommand() : this(new SerializationHelper())
		{

		}

		public UnicornLoadTreeCommand(SerializationHelper helper)
		{
			_helper = helper;
		}

		protected override Item LoadItem(Item item, LoadOptions options)
		{
			Assert.ArgumentNotNull(item, "item");

			IItemData itemData = new ItemData(item);

			var configuration = _helper.GetConfigurationsForItem(itemData).FirstOrDefault(); // if multiple configs contain item, load from first one

			if (configuration == null) return base.LoadItem(item, options);


			var logger = configuration.Resolve<ILogger>();
			var helper = configuration.Resolve<SerializationHelper>();
			var targetDataStore = configuration.Resolve<ITargetDataStore>();

			itemData = targetDataStore.GetByPathAndId(itemData.Path, itemData.Id, itemData.DatabaseName);

			if (itemData == null)
			{
				logger.Warn("Command sync: Could not do partial sync of " + item.Paths.FullPath + " because the root was not serialized.");
				return item;
			}

			try
			{
				logger.Info("Command Sync: Processing partial Unicorn configuration " + configuration.Name + " under " + itemData.Path);

				helper.SyncTree(configuration, partialSyncRoot: itemData);

				logger.Info("Command Sync: Completed syncing partial Unicorn configuration " + configuration.Name + " under " + itemData.Path);
			}
			catch (Exception ex)
			{
				logger.Error(ex);
				throw;
			}

			CorePipeline.Run("unicornSyncEnd", new UnicornSyncEndPipelineArgs(new SitecoreLogger(), true, configuration));

			return Database.GetItem(item.Uri);
		}
	}
}
