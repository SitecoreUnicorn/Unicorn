using System.Collections.Generic;

namespace Unicorn.ControlPanel
{
	public interface IDocumentable
	{
		string FriendlyName { get; }
		string Description { get; }
		KeyValuePair<string, string>[] GetConfigurationDetails();
	}
}
