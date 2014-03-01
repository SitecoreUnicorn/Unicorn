namespace Unicorn.ControlPanel
{
	/// <summary>
	/// Emits a HTML literal into the control panel
	/// </summary>
	/// <remarks>This is kind of a hack, but whatever :)</remarks>
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
