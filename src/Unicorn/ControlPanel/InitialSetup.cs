using System.Web;
using System.Web.UI;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel
{
	public class InitialSetup : IControlPanelControl
	{
		private readonly IPredicate _predicate;

		public InitialSetup(IPredicate predicate)
		{
			_predicate = predicate;
		}

		public string ConfigurationName { get; set; }

		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h4>Initial Setup</h4>");

			if (_predicate.GetRootItems().Length > 0)
			{
				writer.Write("<p>Would you like to perform an initial serialization of all configured items using the options outlined above now? This is required to start using this configuration.</p>");

				writer.Write("<a class=\"button\" href=\"?verb=Reserialize&amp;configuration={0}\">Perform Initial Serialization of <em>{1}</em></a>", HttpUtility.UrlEncode(ConfigurationName), ConfigurationName);
			}
			else
			{
				writer.Write("<p>Cannot perform initial serialization until the predicate configuration includes valid root items.</p>");
			}
		}
	}
}
