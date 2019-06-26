using Configy.Containers;

namespace Unicorn.Configuration
{
	public class MicroConfiguration : MicroContainer, IConfiguration
	{
		public MicroConfiguration(string name, string description, string extends, string[] dependencies, string[] ignoredDependencies) : base(name, extends)
		{
			Description = description;
			Dependencies = dependencies ?? new string[0];
			IgnoredImplicitDependencies = ignoredDependencies ?? new string[0];
		}

		public string Description { get; }
		public string[] Dependencies { get; set; }
		public string[] IgnoredImplicitDependencies { get; set; }
	}
}
