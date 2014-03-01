using System.Collections.Generic;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// An interface that allows documentation to be generated from the implementing type.
	/// 
	/// This is used to allow displaying friendly details about dependencies in the control panel.
	/// </summary>
	public interface IDocumentable
	{
		string FriendlyName { get; }
		string Description { get; }
		KeyValuePair<string, string>[] GetConfigurationDetails();
	}
}
