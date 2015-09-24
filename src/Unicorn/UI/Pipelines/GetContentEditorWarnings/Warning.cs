namespace Unicorn.UI.Pipelines.GetContentEditorWarnings
{
	public class Warning
	{
		public string Message { get; private set; }
		public string Title { get; private set; }

		public Warning(string title, string message)
		{
			Message = message;
			Title = title;
		}
	}
}
