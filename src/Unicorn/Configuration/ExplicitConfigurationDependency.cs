namespace Unicorn.Configuration
{
	internal class ExplicitConfigurationDependency : IConfigurationDependency
	{
		public ExplicitConfigurationDependency(IConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		public IConfiguration Configuration { get; }
		public string GetLogMessage()
		{
			return $"There is an explicit dependency on '{Configuration.Name}' in the configuration.";
		}
	}
}