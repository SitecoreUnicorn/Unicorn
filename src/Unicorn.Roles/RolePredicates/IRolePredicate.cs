using Rainbow;
using Sitecore.Security.Accounts;
using Unicorn.Predicates;

namespace Unicorn.Roles.RolePredicates
{
	public interface IRolePredicate : IDocumentable
	{
		PredicateResult Includes(Role role); 
	}
}
