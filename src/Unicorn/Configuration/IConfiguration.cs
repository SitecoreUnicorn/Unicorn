using Configy.Containers;

namespace Unicorn.Configuration
{
	/// <summary>
	/// Represents a Unicorn configuration. A configuration is basically an instance of a DI container.
	/// </summary>
	public interface IConfiguration : IContainer
	{
		/// <summary>
		/// A description of what this configuration is for. Displayed in control panel if present.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// A list of configuration names on which this configuration depends
		/// </summary>
		string[] Dependencies { get; }

		/// <summary>
		/// A list of configuration names on which this configuration depends
		/// </summary>
		string[] IgnoredImplicitDependencies { get; }
	}
}
