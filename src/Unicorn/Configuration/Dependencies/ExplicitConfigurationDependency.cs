namespace Unicorn.Configuration.Dependencies
{
	/// <summary>
	/// Defines an explicit dependency between two Unicorn configurations
	/// (one that cannot be inferred based on the paths two configurations include)
	/// </summary>
	public class ExplicitConfigurationDependency : IConfigurationDependency
	{
		public ExplicitConfigurationDependency(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public string GetLogMessage()
		{
			return $"There is an explicit dependency on '{Configuration.Name}' in the configuration.";
		}
	}
}