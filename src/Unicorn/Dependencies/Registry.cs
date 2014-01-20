using Sitecore.Configuration;

namespace Unicorn.Dependencies
{
	public static class Registry
	{
		public static readonly IDependencyRegistry Default;
		static Registry()
		{
			Default = new StaticDependencyRegistryWrapper((IDependencyRegistry) Factory.CreateObject("/sitecore/unicorn/dependencyRegistry", true));
		}

		public static IDependencyRegistry CreateCopyOfDefault()
		{
			return (IDependencyRegistry)Factory.CreateObject("/sitecore/unicorn/dependencyRegistry", true);
		}
	}
}
