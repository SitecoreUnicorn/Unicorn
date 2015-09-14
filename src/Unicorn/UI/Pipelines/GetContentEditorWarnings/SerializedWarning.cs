using System.Linq;
using Rainbow.Storage.Sc;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Unicorn.Configuration;
using Unicorn.Data.DataProvider;
using Unicorn.Predicates;

namespace Unicorn.UI.Pipelines.GetContentEditorWarnings
{
	/// <summary>
	/// Pipeline that puts up content editor warnings if the current item is serialized with Unicorn
	/// </summary>
	public class SerializedWarning
	{
		private readonly IConfiguration[] _configurations;

		public SerializedWarning()
			: this(UnicornConfigurationManager.Configurations)
		{
		}

		protected SerializedWarning(IConfiguration[] configurations)
		{
			_configurations = configurations;
		}

		public virtual void Process(GetContentEditorWarningsArgs args)
		{
			Item item = args.Item;
			if (item == null) return;

			var existingSitecoreItem = new ItemData(item);

			if (_configurations.Any(configuration => configuration.Resolve<IPredicate>().Includes(existingSitecoreItem).IsIncluded))
			{
				GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
				warning.Title = RenderTitle(item);
				warning.Text = RenderWarning(item);
			}
		}


		protected virtual string RenderTitle(Item item)
		{
			if (item.Statistics.UpdatedBy == UnicornDataProvider.TransparentSyncUpdatedByValue)
				return "This item is transparently synced by Unicorn";

			return "This item is controlled by Unicorn";
		}

		protected virtual string RenderWarning(Item item)
		{
			if (Settings.GetBoolSetting("Unicorn.DevMode", true))
				return "Changes to this item will be written to disk so they can be committed to source control and shared with others.";

			return "You should not change this item because your changes may be overwritten by the next code deployment.";
		}
	}
}