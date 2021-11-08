using System;

namespace Unicorn.Predicates.Exclusions
{
	internal static class PathTool
	{
		internal static bool ComparePathSegments(string[] path1, string[] path2, bool allowPartialMatch = false)
		{
			if (!allowPartialMatch && path1?.Length != path2?.Length)
			{
				return false;
			}

			var segments = Math.Min(path1.Length, path2.Length);
			for (var i = 0; i < segments; i++)
			{
				var segment1 = path1[i];
				var segment2 = path2[i];

				const string wildcard = "*";
				if (wildcard.Equals(segment1, StringComparison.Ordinal) 
					|| wildcard.Equals(segment2, StringComparison.Ordinal))
				{
					continue;
				}

				if (!string.Equals(segment1, segment2, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		internal static string[] ExplodePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return new string[] {};
			}

			var safePath = EnsureTrailingSlash(path);
			var pathSegments = safePath.Split(new[] { '/' } , StringSplitOptions.RemoveEmptyEntries);

			return pathSegments;
		}

		internal static string EnsureTrailingSlash(string path)
		{
			if (path.EndsWith("/")) return path;
			return path + "/";
		}
	}
}
