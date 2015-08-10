using System.Linq;
using Rainbow.Model;
using Rainbow.Storage.Sc;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Unicorn.Configuration;
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
				warning.Title = RenderTitle(existingSitecoreItem);
				warning.Text = RenderWarning(existingSitecoreItem);
			}
		}


		protected virtual string RenderTitle(IItemData item)
		{
			return "This item is controlled by Unicorn";
		}

		protected virtual string RenderWarning(IItemData item)
		{
			if (Settings.GetBoolSetting("Unicorn.DevMode", true))
				return "Changes to this item will be written to disk so they can be committed to source control and shared with others.";

			return "You should not change this item because your changes may be overwritten by the next code deployment.";
		}
	}
}