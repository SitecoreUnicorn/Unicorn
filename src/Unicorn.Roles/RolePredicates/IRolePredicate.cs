using Rainbow;
using Sitecore.Security.Accounts;
using Unicorn.Predicates;
using Unicorn.Roles.Model;

namespace Unicorn.Roles.RolePredicates
{
	public interface IRolePredicate : IDocumentable
	{
		PredicateResult Includes(IRoleData role); 
	}
}
