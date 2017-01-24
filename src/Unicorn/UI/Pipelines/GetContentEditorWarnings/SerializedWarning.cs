using System.Linq;
using Rainbow.Storage.Sc;
using Sitecore.Configuration;
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

			IConfiguration foundConfiguration = null;
			PredicateResult foundPredicateResult = null;

			foreach (var configuration in _configurations)
			{
				var predicate = configuration.Resolve<IPredicate>();
				var predicateResult = predicate.Includes(existingSitecoreItem);
				if (predicateResult.IsIncluded)
				{
					foundConfiguration = configuration;
					foundPredicateResult = predicateResult;
					break;
				}
			}

			if (foundConfiguration != null)
			{
				var evaluator = foundConfiguration.Resolve<IEvaluator>();

				var warningObject = evaluator.EvaluateEditorWarning(item);

				if (warningObject != null)
				{
					GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
					warning.Title = warningObject.Title;
					if (Settings.GetBoolSetting("Unicorn.DevMode", true))
						warning.Text = string.Format($"{warningObject.Message} Predicate name: '{foundPredicateResult.PresetTreeRoot?.Name}'.");
					else
						warning.Text = warningObject.Message;
				}
			}
		}
	}
}