using Unicorn.Predicates;

namespace Unicorn
{
	public static class SerializationUtility
	{
		/// <summary>
		/// Gets the default presets specified in configuration/sitecore/serialization/default
		/// </summary>
		public static IPredicate GetDefaultPreset()
		{
			return new SerializationPresetPredicate("default");
		}
	}
}
