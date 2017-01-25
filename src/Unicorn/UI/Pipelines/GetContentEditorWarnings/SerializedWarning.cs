using System.Linq;
using Rainbow.Storage.Sc;
using Sitecore.Data.Items;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Unicorn.Configuration;
using Unicorn.Evaluators;
using Unicorn.Predicates;
// ReSharper disable TooWideLocalVariableScope

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

			PredicateResult matchingPredicate = null;

			foreach (var configuration in _configurations)
			{
				matchingPredicate = configuration.Resolve<IPredicate>().Includes(existingSitecoreItem);

				if (matchingPredicate.IsIncluded)
				{
					var evaluator = configuration.Resolve<IEvaluator>();

					var warningObject = evaluator.EvaluateEditorWarning(item, matchingPredicate);

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
}