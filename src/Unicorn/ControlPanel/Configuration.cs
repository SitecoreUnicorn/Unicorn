using System.Web.UI;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Renders the current dependency/provider configuration for Unicorn, using IDocumentable to show additional details when available.
	/// </summary>
	public class Configuration : IControlPanelControl
	{
		private readonly IPredicate _predicate;
		private readonly ISerializationProvider _serializationProvider;
		private readonly ISourceDataProvider _sourceDataProvider;
		private readonly IEvaluator _evaluator;

		public Configuration(IPredicate predicate, ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, IEvaluator evaluator)
		{
			_predicate = predicate;
			_serializationProvider = serializationProvider;
			_sourceDataProvider = sourceDataProvider;
			_evaluator = evaluator;
		}

		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h2>Current Configuration</h2>");

			RenderType("Predicate", 
				"Predicates define what items get included into Unicorn, because you don't want to serialize everything. You can implement your own to use any criteria for inclusion you can possibly imagine.", 
				_predicate, 
				writer);

			RenderType("Serialization Provider",
				"Defines how items are serialized - for example, using standard Sitecore serialization APIs, JSON to disk, XML in SQL server, etc",
				_serializationProvider,
				writer);

			RenderType("Source Data Provider",
				"Defines how source data is read to compare with serialized data. Normally this is a Sitecore database.",
				_sourceDataProvider,
				writer);

			RenderType("Evaluator",
				"The evaluator decides what to do when included items need to be evaluated to see if they need updating, creation, or deletion.",
				_evaluator,
				writer);

		}

		private void RenderType(string categorization, string categoryDescription, object type, HtmlTextWriter writer)
		{
			var documentable = type as IDocumentable;

			writer.RenderBeginTag("fieldset");
				writer.RenderBeginTag("legend");
					writer.Write(categorization);
					RenderHelp(categoryDescription, writer);
				writer.RenderEndTag();

				writer.RenderBeginTag("h4");
					if (documentable == null)
					{
						writer.Write(type.GetType().Name + " (does not implement IDocumentable)");
						writer.RenderEndTag();
						return;
					}

					writer.Write(documentable.FriendlyName);
					writer.Write(" <pre>({0})</pre>", type.GetType().FullName);
				writer.RenderEndTag();

				if (!string.IsNullOrWhiteSpace(documentable.Description))
					writer.Write("<p>{0}</p>", documentable.Description);

				var configuration = documentable.GetConfigurationDetails();

				if (configuration == null || configuration.Length == 0)
				{
					writer.RenderEndTag();
					return;
				}

				writer.RenderBeginTag("h5");
					writer.Write("Configuration Details");
				writer.RenderEndTag();

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
