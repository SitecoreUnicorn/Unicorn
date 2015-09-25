using System.Web.UI;
using Rainbow;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel.Controls
{
	/// <summary>
	/// Renders the current dependency/provider configuration for Unicorn, using IDocumentable to show additional details when available.
	/// </summary>
	internal class ConfigurationDetails : IControlPanelControl
	{
		private readonly IPredicate _predicate;
		private readonly ITargetDataStore _serializationStore;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly IEvaluator _evaluator;

		public ConfigurationDetails(IPredicate predicate, ITargetDataStore serializationStore, ISourceDataStore sourceDataStore, IEvaluator evaluator)
		{
			_predicate = predicate;
			_serializationStore = serializationStore;
			_sourceDataStore = sourceDataStore;
			_evaluator = evaluator;
		}

		public string ConfigurationName { get; set; }
		public string ModalId { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			bool collapse = !string.IsNullOrWhiteSpace(ModalId);

			if (collapse) writer.Write(@"
				<div id=""{0}"" class=""overlay"">", ModalId);

			writer.Write(@"
					<article class=""modal"">");

			if(collapse)
				writer.Write(@"
						<h2>{0} Details</h2>", ConfigurationName);

			RenderType(collapse,
				"Predicate", 
				"Predicates define which items are included or excluded in Unicorn.", 
				_predicate, 
				writer);

			RenderType(collapse,
				"Target Data Store",
				"Defines how items are serialized, for example to disk using YAML format.",
				_serializationStore,
				writer);

			RenderType(collapse,
				"Source Data Store",
				"Defines how source data is read to compare with serialized data. Normally this is a Sitecore data store.",
				_sourceDataStore,
				writer);

			RenderType(collapse,
				"Evaluator",
				"The evaluator decides what to do when included items need updating, creation, or deletion.",
				_evaluator,
				writer);

			writer.Write(@"
					</article>");

			if (collapse) writer.Write(@"
				</div>");
		}

		private void RenderType(bool collapsed, string categorization, string categoryDescription, object type, HtmlTextWriter writer)
		{
			writer.Write(@"
				<section>");

			writer.Write(@"
					<h{0}>{1}</h{0}>", collapsed ? 3 : 4, categorization);

			writer.Write(@"
					<p class=""help"">{0}</p>", categoryDescription);

			writer.Write(@"
					<h4>{0}</h4>", DocumentationUtility.GetFriendlyName(type));

			var description = DocumentationUtility.GetDescription(type);
			if (!string.IsNullOrWhiteSpace(description))
					writer.Write(@"
					<p>{0}</p>", description);

			var configuration = DocumentationUtility.GetConfigurationDetails(type);

			if (configuration != null && configuration.Length > 0)
			{
				writer.Write(@"
					<ul>");

				foreach (var config in configuration)
				{
					writer.Write(@"
						<li><strong>{0}</strong>: {1}</li>", config.Key, config.Value);
				}

				writer.Write(@"
					</ul>");
			}

			writer.Write(@"
				</section>");
		}
	}
}
