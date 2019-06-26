using System;
using Rainbow.Model;

namespace Unicorn.Predicates.Exclusions
{
	/// <summary>
	/// Excludes a a specific path, and all children thereof, from a configuration
	/// e.g. <exclude path="/foo/bar" /> in config
	/// </summary>
	public class PathBasedPresetTreeExclusion : IPresetTreeExclusion
	{
		private readonly string _excludedPath;
		private readonly bool _implicitChildrenExclusion;

		public PathBasedPresetTreeExclusion(string excludedPath, PresetTreeRoot root)
		{
			_excludedPath = excludedPath;

			// path that does not start with / is relative to the parent include root path
			if (!_excludedPath.StartsWith("/")) _excludedPath = $"{root.Path.TrimEnd('/')}/{_excludedPath}";

			// for legacy compatibility you can exclude children by having a path exclusion end with a trailing slash
			// but since we add a trailing slash internally to all paths (so that we match by path e.g. /foo != /foot)
			// we need to know if the original string had a trailing slash and handle it as a child exclusion
			_implicitChildrenExclusion = _excludedPath.EndsWith("/");

			// we internally add a trailing slash to the excluded path, and add a trailing slash to the incoming evaluate path
			// why? because otherwise using StartsWith would mean that /foo would also exclude /foot. But /foo/ and /foot/ do not match like that.
			_excludedPath = PathTool.EnsureTrailingSlash(_excludedPath);
		}

		public PredicateResult Evaluate(IItemData itemData)
		{
			var itemPath = PathTool.EnsureTrailingSlash(itemData.Path);

			bool result = itemPath.StartsWith(_excludedPath, StringComparison.OrdinalIgnoreCase);

			// if we have implicit children, due to appending trailing slashes the root where we are excluding children will initially match as excluded
			// so we have an explicit check for equality between an implicit children path and the current path - which in the case of implicit children means
			// we want to include it (result = false) instead of the default exclusion
			if (_implicitChildrenExclusion && result && itemPath.Equals(_excludedPath, StringComparison.OrdinalIgnoreCase))
				result = false;

			return result
						? new PredicateResult($"Item path exclusion rule: {_excludedPath}")
						: new PredicateResult(true);
		}

		// ReSharper disable once ConvertToAutoProperty
		public string Description => _excludedPath;
	}
}
