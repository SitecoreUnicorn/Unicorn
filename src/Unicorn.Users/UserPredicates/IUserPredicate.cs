using Sitecore.Security.Accounts;
using Unicorn.Predicates;

namespace Unicorn.Users.UserPredicates
{
	public interface IUserPredicate
	{
		PredicateResult Includes(User user);
	}
}
