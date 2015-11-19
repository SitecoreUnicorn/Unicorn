using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data.DataProvider;
using Unicorn.Evaluators;
using Unicorn.Predicates;

namespace Unicorn.UI.Pipelines.SaveUi
{
	/// <summary>
	/// Provides a saveUI pipeline implementation to prevent changing a Unicorn-controlled item when on a deployed environment (CE)
	/// 
	/// Do not enable this pipeline on development instances or you will be unable to save any Unicorn items :)
	/// </summary>
	public class SerializationChangeBlocker : SaveUiConfirmProcessor
	{
		private readonly IConfiguration[] _configurations;

		public SerializationChangeBlocker()
			: this(UnicornConfigurationManager.Configurations)
		{
		}

		protected SerializationChangeBlocker(IConfiguration[] configurations)
		{
			_configurations = configurations;
		}

		protected override string GetDialogText(SaveArgs args)
		{
			foreach (var item in args.Items)
			{
				Item existingItem = Client.ContentDatabase.GetItem(item.ID, item.Language, item.Version);

				Assert.IsNotNull(existingItem, "Existing item {0} did not exist! This should never occur.", item.ID);

				var existingSitecoreItem = new ItemData(existingItem);

				if (_configurations.Any(configuration =>
				{
					if (!configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync &&
						configuration.Resolve<IPredicate>().Includes(existingSitecoreItem).IsIncluded &&
						configuration.Resolve<IEvaluator>().ShouldPerformConflictCheck(existingItem))
						return true;

					return false;
				}))
				{
					return GetMessage(existingSitecoreItem);
				}
			}

			return null;
		}

		protected virtual string GetMessage(IItemData item)
		{
			return "You should not edit {0} because it is controlled by Unicorn. Future code deployments will probably undo your change. Are you sure about saving?".FormatWith(item.Name);
		}
	}
}
