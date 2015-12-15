namespace Unicorn.Configuration.Dependencies
{
	/// <summary>
	/// Defines a dependency between two Unicorn configurations
	/// </summary>
	public interface IConfigurationDependency
	{
		/// <summary>
		/// Which configuration is it dependent on.
		/// </summary>
		IConfiguration Configuration { get; }

		/// <summary>
		/// Returns a human readable string describing the dependency 
		/// </summary>
		string GetLogMessage();
	}
}