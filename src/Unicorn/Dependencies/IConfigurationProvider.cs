namespace Unicorn.Dependencies
{
	public interface IConfigurationProvider
	{
		IConfiguration[] Configurations { get; }
	}
}
