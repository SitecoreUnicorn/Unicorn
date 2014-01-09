using System.Web.UI;
using Kamsar.WebConsole;
using Unicorn.Dependencies;
using Unicorn.Evaluators;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.ControlPanel
{
	public class Configuration : IControlPanelControl
	{
		public void Render(HtmlTextWriter writer)
		{
			// but of a hack - default config depends on the reg of one of these
			Registry.Current.RegisterInstanceFactory<IProgressStatus>(() => new StringProgressStatus());

			writer.Write("<h2>Current Configuration</h2>");

			RenderType("Predicate", 
				"Predicates define what items get included into Unicorn, because you don't want to serialize everything. You can implement your own to use any criteria for inclusion you can possibly imagine.", 
				Registry.Current.Resolve<IPredicate>(), 
				writer);

			RenderType("Serialization Provider",
				"Defines how items are serialized - for example, using standard Sitecore serialization APIs, JSON to disk, XML in SQL server, etc",
				Registry.Current.Resolve<ISerializationProvider>(),
				writer);

			RenderType("Evaluator",
				"The evaluator decides what to do when included items need to be evaluated to see if they need updating, creation, or deletion.",
				Registry.Current.Resolve<IEvaluator>(),
				writer);

		}

		private void RenderType(string categorization, string categoryDescription, object type, HtmlTextWriter writer)
		{
			var documentable = type as IDocumentable;

			writer.RenderBeginTag("h3");

				writer.Write(categorization);
				RenderHelp(categoryDescription, writer);

				writer.Write(": ");

				if (documentable == null)
				{
					writer.Write(type.GetType().FullName + " (does not implement IDocumentable)");
					return;
				}

				writer.Write(documentable.FriendlyName);
				if (!string.IsNullOrWhiteSpace(documentable.Description))
					RenderHelp(documentable.Description, writer);
			writer.RenderEndTag();

			var configuration = documentable.GetConfigurationDetails();

			if (configuration == null || configuration.Length == 0) return;

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
