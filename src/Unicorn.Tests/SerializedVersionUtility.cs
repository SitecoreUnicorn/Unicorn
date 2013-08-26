using System;
using System.Linq;
using Sitecore;
using Unicorn.Serialization;

namespace Unicorn.Tests
{
	internal static class SerializedVersionUtility
	{
		internal static SerializedVersion CreateTestVersion(string language, int version, DateTime modified, string revision)
		{
			var serializedVersion = new SerializedVersion(language, version);
			if (modified != default(DateTime))
				serializedVersion.Fields[FieldIDs.Updated.ToString()] = DateUtil.ToIsoDate(modified);

			if (!string.IsNullOrEmpty(revision))
				serializedVersion.Fields[FieldIDs.Revision.ToString()] = revision;

			return serializedVersion;
		}
	}
}
