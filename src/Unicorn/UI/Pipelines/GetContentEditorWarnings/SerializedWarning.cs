using System.Linq;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Unicorn.Configuration;
using Unicorn.Evaluators;
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

			var configuration = _configurations.FirstOrDefault(config => config.Resolve<IPredicate>().Includes(existingSitecoreItem).IsIncluded);
			if (configuration != null)
			{
				var evaluator = configuration.Resolve<IEvaluator>();

				var warningObject = evaluator.EvaluateEditorWarning(item);

				if (warningObject != null)
				{
					GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
					warning.Title = warningObject.Title;
					warning.Text = warningObject.Message;
				}
			}
		}
	}
}