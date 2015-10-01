using Sitecore.Common;

namespace Unicorn
{
	/// <summary>
	/// Forces the Unicorn data provider to enable serialization, regardless of other disablers
	/// but only for the current thread (as opposed to globally like DataProvider.DisableSerialization)
	/// 
	/// Used to enable getting templates using TpSync during deserialization
	/// </summary>
	public class SerializationEnabler : Switcher<bool, SerializationEnabler>
	{
		public SerializationEnabler() : base(true)
		{
		}
	}
}
