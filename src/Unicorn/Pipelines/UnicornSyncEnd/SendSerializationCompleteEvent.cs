using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Jobs;
using Unicorn.Predicates;

// ReSharper disable UnusedMember.Global

namespace Unicorn.Pipelines.UnicornSyncEnd
{
	public class SendSerializationCompleteEvent : IUnicornSyncEndProcessor
	{
		public void Process(UnicornSyncEndPipelineArgs args)
		{
			var databases = args.SyncedConfigurations
				.SelectMany(config => config.Resolve<IPredicate>().GetRootPaths())
				.Select(path => path.DatabaseName)
				.Distinct();

			foreach (var database in databases)
			{
				DeserializationComplete(database);
			}
		}

		protected virtual void DeserializationComplete(string databaseName)
		{
			Assert.ArgumentNotNullOrEmpty(databaseName, "databaseName");

			// raising this event can take a long time. like 16 seconds. So we boot it as a job so it can go in the background.
			Job asyncSerializationFinished = new Job(new JobOptions("Raise deserialization complete async", "serialization", "shell", this, "RaiseEvent", new object[] { databaseName }));
			JobManager.Start(asyncSerializationFinished);
		}

		public virtual void RaiseEvent(string databaseName)
		{
			EventManager.RaiseEvent(new SerializationFinishedEvent());
			Database database = Factory.GetDatabase(databaseName, false);

			database?.RemoteEvents.Queue.QueueEvent(new SerializationFinishedEvent());
		}
	}
}
