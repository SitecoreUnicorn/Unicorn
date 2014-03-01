namespace Unicorn.Configuration
{
	/// <summary>
	/// This interface provides the set of configurations that Unicorn will run with.
	/// </summary>
	public interface IConfigurationProvider
	{
		IConfiguration[] Configurations { get; }
	}
}
