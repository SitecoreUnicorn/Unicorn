using Sitecore.Configuration;

namespace Unicorn.Dependencies
{
	public static class Registry
	{
		public static readonly IDependencyRegistry Current;
		static Registry()
		{
			Current = (IDependencyRegistry) Factory.CreateObject("/sitecore/unicorn/dependencyRegistry", true);
		}
	}
}
