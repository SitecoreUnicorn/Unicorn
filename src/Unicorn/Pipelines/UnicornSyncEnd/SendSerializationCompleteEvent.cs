using System.Linq;
using System.Reflection;
using System.Threading;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Sites;
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

			Log.Info($"Job started: Raise deserialization complete async ({typeof(SendSerializationCompleteEvent).FullName})", this);
			ThreadPool.QueueUserWorkItem(RaiseEvent, databaseName);
		}

		public virtual void RaiseEvent(object state)
		{
			using (new SiteContextSwitcher(SiteContext.GetSite("shell")))
			{
				var databaseName = state.ToString();
				EventManager.RaiseEvent(new SerializationFinishedEvent());
				Database database = Factory.GetDatabase(databaseName, false);
				
				var remoteEvents = database?.RemoteEvents;
				if (remoteEvents == null)
				{
					Log.Warn("Remote Events not configured for this instance. SerializationFinishedEvent is not raised.", this);
					return;
				}

				var remoteEventsType = remoteEvents.GetType();

				// Pre 9.3, you could access the Queue property
				// 9.3+ you need to access the EventQueue property

				// All of this in the Sitecore.Data.Eventing.DatabaseRemoteEvents

				// Try for 9.3 (some versions of 9.x also has this property)
				var eventQueue = remoteEventsType?.GetProperty("EventQueue", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
				if (eventQueue == null)
				{
					// Try for v.Prev
					eventQueue = remoteEventsType?.GetProperty("Queue", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
				}

				if (eventQueue == null)
				{
					Log.Error("Cannot locate Queue/EventQueue property on [Database].RemoteEvents. SerializationFinishedEvent is not raised.", this);
					return;
				}
				var eventQueueInstance = eventQueue.GetValue(remoteEvents);

				eventQueue?.GetType()?.GetMethod("QueueEvent")?.Invoke(eventQueueInstance, new object[] { new SerializationFinishedEvent() });				
			}

			Log.Info($"Job ended: Raise deserialization complete async ({typeof(SendSerializationCompleteEvent).FullName})", this);
		}
	}
}
