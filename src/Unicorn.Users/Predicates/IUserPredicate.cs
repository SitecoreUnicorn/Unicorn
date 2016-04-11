using Sitecore.Security.Accounts;
using Unicorn.Predicates;

namespace Unicorn.Users.Predicates
{
	public interface IUserPredicate
	{
		PredicateResult Includes(User user);
	}
}
