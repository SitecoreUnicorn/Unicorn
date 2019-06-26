using Unicorn.Configuration;

namespace Unicorn.Users.Loader
{
	public interface IUserLoader
	{
		void Load(IConfiguration configuration);
	}
}