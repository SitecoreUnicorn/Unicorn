namespace Unicorn.Users.Pipelines.UnicornSyncComplete
{
  using Unicorn.Pipelines.UnicornSyncComplete;
  using Loader;
  using Predicates;

  public class SyncUsers : IUnicornSyncCompleteProcessor
  {
    public void Process(UnicornSyncCompletePipelineArgs args)
    {
      var userPredicate = args.Configuration.Resolve<IUserPredicate>();

      // no predicate = configuration doesn't include any roles
      if (userPredicate == null) return;

      var loader = args.Configuration.Resolve<IUserLoader>();

      //logger.Info(string.Empty);
      //logger.Info($"{args.Configuration.Name} roles are being synced.");

      loader.Load(args.Configuration);

      //logger.Info($"{args.Configuration.Name} role sync complete.");
    }
  }
}
