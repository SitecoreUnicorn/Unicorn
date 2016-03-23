namespace Unicorn.Users.Loader
{
  using Unicorn.Configuration;
  public interface IUserLoader
	{
		void Load(IConfiguration configuration);
	}
}