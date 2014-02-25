namespace Unicorn.ControlPanel
{
	internal class Literal : IControlPanelControl
	{
		private readonly string _literalHtml;

		public Literal(string literalHtml)
		{
			_literalHtml = literalHtml;
		}

		public void Render(System.Web.UI.HtmlTextWriter writer)
		{
			writer.Write(_literalHtml);
		}
	}
}
