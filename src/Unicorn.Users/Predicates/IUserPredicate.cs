namespace Unicorn.Users.Predicates
{
  using Sitecore.Security.Accounts;
  using Unicorn.Predicates;

  public interface IUserPredicate
  {
    PredicateResult Includes(User user);
  }
}
