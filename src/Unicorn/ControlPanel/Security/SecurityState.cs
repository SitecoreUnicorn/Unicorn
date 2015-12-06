namespace Unicorn.ControlPanel.Security
{
	public class SecurityState
	{
		public SecurityState(bool allowed, bool automated)
		{
			IsAllowed = allowed;
			IsAutomatedTool = automated;
		}

		public bool IsAllowed { get; private set; }
		public bool IsAutomatedTool { get; private set; }
	}
}
