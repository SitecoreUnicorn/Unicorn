using Sitecore.Configuration;
using Sitecore.Data.Serialization.Presets;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Unicorn
{
	public static class SerializationUtility
	{
		/// <summary>
		/// Gets the presets specified in configuration/sitecore/serialization
		/// </summary>
		public static IList<IncludeEntry> GetPreset()
		{
			var config = Factory.GetConfigNode("serialization");
			if (config != null) 
				return config.ChildNodes.OfType<XmlNode>().SelectMany(GetPreset).ToList();

			return null;
		}

		/// <summary>
		/// Gets the presets specified in configuration/sitecore/serialization/name
		/// </summary>
		public static IList<IncludeEntry> GetPreset(string name)
		{
			var config = Factory.GetConfigNode("serialization/" + name);
			return GetPreset(config);
		}

		/// <summary>
		/// Gets the presets of a specific XmlNode
		/// </summary>
		public static IList<IncludeEntry> GetPreset(XmlNode config)
		{
			if (config != null)
				return PresetFactory.Create(config);

			return null;
		}
	}
}
