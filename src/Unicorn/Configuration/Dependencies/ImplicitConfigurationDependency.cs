namespace Unicorn.Configuration.Dependencies
{
	/// <summary>
	/// Defines an implicit dependency between Unicorn configurations
	/// e.g. if A contains /sitecore/content/Home
	/// and B contains /sitecore/content/Home/Foo,
	/// then B implicitly depends on A
	/// </summary>
	public class ImplicitConfigurationDependency : IConfigurationDependency
	{
		public ImplicitConfigurationDependency(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public string GetLogMessage()
		{
			return $"There is an implicit dependency on '{Configuration.Name}' because it contains a parent path of a path included in this configuration.";
		}
	}
}