using System.Web.UI;
using Rainbow.Formatting;
using Rainbow.Storage;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Renders the current dependency/provider configuration for Unicorn, using IDocumentable to show additional details when available.
	/// </summary>
	public class ConfigurationDetails : IControlPanelControl
	{
		private readonly IPredicate _predicate;
		private readonly IDataStore _serializationStore;
		private readonly ISourceDataStore _sourceDataProvider;
		private readonly ISerializationFormatter _formatter;
		private readonly IEvaluator _evaluator;

		public ConfigurationDetails(IPredicate predicate, IDataStore serializationStore, ISourceDataStore sourceDataProvider, IEvaluator evaluator, ISerializationFormatter formatter)
		{
			_predicate = predicate;
			_serializationStore = serializationStore;
			_sourceDataProvider = sourceDataProvider;
			_evaluator = evaluator;
			_formatter = formatter;
		}

		public string ConfigurationName { get; set; }
		public bool CollapseByDefault { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			if (CollapseByDefault) writer.Write("<h4 class=\"expand\">{0} Details</h4>", ConfigurationName);
			else writer.Write("<h4>{0} Details</h4>", ConfigurationName);

			if(CollapseByDefault) writer.AddAttribute("class", "details collapsed");
			else writer.AddAttribute("class", "details");

			writer.RenderBeginTag("ul");

			RenderType("Predicate", 
				"Predicates define what items get included into Unicorn, because you don't want to serialize everything. You can implement your own to use any criteria for inclusion you can possibly imagine.", 
				_predicate, 
				writer);

			RenderType("Serialization Provider",
				"Defines how items are serialized - for example, using standard Sitecore serialization APIs, JSON to disk, XML in SQL server, etc",
				_serializationStore,
				writer);

			if (_formatter != null)
			{
				RenderType("Serialization Formatter",
					"Defines the format the serialization provider writes serialized items with.",
					_formatter,
					writer);
			}

			RenderType("Source Data Provider",
				"Defines how source data is read to compare with serialized data. Normally this is a Sitecore database.",
				_sourceDataProvider,
				writer);

			RenderType("Evaluator",
				"The evaluator decides what to do when included items need to be evaluated to see if they need updating, creation, or deletion.",
				_evaluator,
				writer);

			writer.RenderEndTag(); // ul
		}

		private void RenderType(string categorization, string categoryDescription, object type, HtmlTextWriter writer)
		{
			var documentable = type as IDocumentable;

			writer.RenderBeginTag("li");
				writer.RenderBeginTag("h5");
					writer.Write(categorization);
					RenderHelp(categoryDescription, writer);
				writer.RenderEndTag();

				writer.RenderBeginTag("p");
					writer.RenderBeginTag("strong");
						if (documentable == null)
						{
							writer.Write(type.GetType().Name + " (does not implement IDocumentable)");
							writer.RenderEndTag();
							return;
						}

						writer.Write(documentable.FriendlyName);
					writer.RenderEndTag();

					writer.Write(" <code>({0})</code>", type.GetType().FullName);
				writer.RenderEndTag();

				if (!string.IsNullOrWhiteSpace(documentable.Description))
					writer.Write("<p>{0}</p>", documentable.Description);

				var configuration = documentable.GetConfigurationDetails();

				if (configuration == null || configuration.Length == 0)
				{
					writer.RenderEndTag();
					return;
				}

				writer.RenderBeginTag("ul");
					foreach (var config in configuration)
					{
						writer.RenderBeginTag("li");
							writer.RenderBeginTag("strong");
								writer.Write(config.Key);
							writer.RenderEndTag();

							writer.Write(": ");
							writer.Write(config.Value);
						writer.RenderEndTag();
					}
				writer.RenderEndTag();
			writer.RenderEndTag();
		}

		private void RenderHelp(string help, HtmlTextWriter writer)
		{
			writer.AddAttribute("title", help);
			writer.AddAttribute("href", "#");
			writer.RenderBeginTag("a");
				writer.Write("[?]");
			writer.RenderEndTag();
		}
	}
}
