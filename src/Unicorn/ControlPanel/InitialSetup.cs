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

		public void Render(HtmlTextWriter writer)
		{
			writer.Write("<h2>Initial Setup</h2>");

			if (_predicate.GetRootItems().Length > 0)
			{
				writer.Write("<p>Would you like to perform an initial serialization of all configured items using the options outlined above now? This is required to start using Unicorn.</p>");

				writer.Write("<a class=\"button\" href=\"?verb=Reserialize\">Perform Initial Serialization</a>");
			}
			else
			{
				writer.Write("<p>Cannot perform initial serialization until the predicate configuration includes valid root items.</p>");
			}
		}
	}
}
