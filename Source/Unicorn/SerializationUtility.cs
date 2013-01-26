using System.Collections.Generic;
using Sitecore.Configuration;
using Sitecore.Data.Serialization.Presets;

namespace Unicorn
{
	public static class SerializationUtility
	{
		/// <summary>
		/// Gets the default presets specified in configuration/sitecore/serialization/default
		/// </summary>
		public static IList<IncludeEntry> GetPreset()
		{
			return GetPreset("default");
		}

		/// <summary>
		/// Gets the default presets specified in configuration/sitecore/serialization/name
		/// </summary>
		public static IList<IncludeEntry> GetPreset(string name)
		{
			var config = Factory.GetConfigNode("serialization/" + name);
			if(config != null)
				return PresetFactory.Create(config);

			return null;
		}
	}
}
