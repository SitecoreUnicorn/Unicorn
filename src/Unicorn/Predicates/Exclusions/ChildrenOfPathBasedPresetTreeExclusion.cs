using System;
using System.Linq;
using Rainbow.Model;

namespace Unicorn.Predicates.Exclusions
{
	/// <summary>
	/// Excludes children of a specific sub-path while including the parent of the sub-path
	/// e.g. for include root /foo, this would let you include /foo/bar/baz while excluding baz' children
	/// in config <exclude childrenOfPath="/path"/> or <exclude children="true" /> (sugar for childrenOfPath = root path)
	/// </summary>
	public class ChildrenOfPathBasedPresetTreeExclusion : IPresetTreeExclusion
	{
		private readonly string[] _exceptions;
		private readonly string _excludeChildrenOfPath;

		public ChildrenOfPathBasedPresetTreeExclusion(string excludeChildrenOfPath, string[] exceptions, PresetTreeRoot root)
		{
			_excludeChildrenOfPath = excludeChildrenOfPath;

			// path that does not start with / is relative to the parent include root path
			// eg include /foo, path 'bar' => /foo/bar
			if (!_excludeChildrenOfPath.StartsWith("/")) _excludeChildrenOfPath = $"{root.Path.TrimEnd('/')}/{_excludeChildrenOfPath}";

			// normalize the root path to have a trailing slash which will make the path match only children
			// (like implicit matching)
			_excludeChildrenOfPath = PathTool.EnsureTrailingSlash(_excludeChildrenOfPath);

			// convert all exceptions to full paths with a trailing slash (so we can match on path segments)
			_exceptions = exceptions.Select(exception => $"{PathTool.EnsureTrailingSlash(_excludeChildrenOfPath)}{exception}/").ToArray();
		}

		public PredicateResult Evaluate(IItemData itemData)
		{
			var itemPath = PathTool.EnsureTrailingSlash(itemData.Path);

			// you may preserve certain children from exclusion
			foreach (var exception in _exceptions)
			{
				var unescapedException = exception.Replace(@"\*", "*");
				if (itemPath.StartsWith(unescapedException, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(true);
			}

			// if the path isn't under the exclusion, it's included
			var unescapedWildcardFreePath = _excludeChildrenOfPath.EndsWith("/*/") ? _excludeChildrenOfPath.Substring(0, _excludeChildrenOfPath.Length - 2) : _excludeChildrenOfPath;
			// unescape any "\*" escapes to match a literal wildcard item so we can compare the path (we don't check this variable for * later)
			unescapedWildcardFreePath = unescapedWildcardFreePath.Replace(@"\*", "*");

			if (!itemPath.StartsWith(unescapedWildcardFreePath, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(true);

			// if the path EQUALS the exclusion path it's included. Because we're including the root, and excluding the children.
			if (itemPath.Equals(unescapedWildcardFreePath, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(true);

			// if the path EQUALS a wildcarded exclusion path it's included.
			// we accomplish this by doing an equals on the parent path of both the item path and the exclusion
			// /foo/bar => /foo, then match against COP = /foo/* => /foo/ == TRUE, so we include it
			// but, /foo/bar/baz => /foo/bar, match against COP /foo/* => /foo/ == FALSE, so it is excluded
			if (_excludeChildrenOfPath.EndsWith("/*/"))
			{
				var itemParentPath = itemPath.Substring(0, itemPath.TrimEnd('/').LastIndexOf('/') + 1); // /foo/bar/ => /foo/
				if (itemParentPath.Equals(unescapedWildcardFreePath, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(true);
			}

			// the item is part of the exclusion
			return new PredicateResult($"Children of {_excludeChildrenOfPath} excluded");
		}

		public string Description => $"children of {_excludeChildrenOfPath}";
	}
}
