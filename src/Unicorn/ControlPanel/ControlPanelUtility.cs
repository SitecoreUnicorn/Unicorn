using Unicorn.Configuration;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel
{
	public static class ControlPanelUtility
	{
		/// <summary>
		/// Checks if any of the current predicate's root paths exist in the serialization provider
		/// </summary>
		public static bool HasAnySerializedItems(IConfiguration configuration)
		{
			return configuration.Resolve<PredicateRootPathResolver>().GetRootSerializedItems().Length > 0;
		}

		/// <summary>
		/// Checks if any of the current predicate's root paths exist in the source provider
		/// </summary>
		public static bool HasAnySourceItems(IConfiguration configuration)
		{
			return configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems().Length > 0;
		}
	}
}
