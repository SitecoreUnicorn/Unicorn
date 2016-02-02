using Unicorn.Configuration;

namespace Unicorn.Roles.Loader
{
	public interface IRoleLoader
	{
		void Load(IConfiguration configuration);
	}
}