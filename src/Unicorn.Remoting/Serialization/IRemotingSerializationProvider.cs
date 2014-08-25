namespace Unicorn.Remoting.Serialization
{
	/// <summary>
	/// Marker interface used to denote a serialization provider who should have its remote updated before a sync, if it's in use
	/// </summary>
	public interface IRemotingSerializationProvider
	{
		string RemoteUrl { get; }
	}
}
