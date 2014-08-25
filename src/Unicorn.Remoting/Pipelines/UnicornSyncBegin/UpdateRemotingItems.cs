using System;
using System.IO;
using System.Net;
using System.Web.Hosting;
using Sitecore.ApplicationCenter.Applications;
using Sitecore.Configuration;
using Sitecore.StringExtensions;
using Unicorn.Logging;
using Unicorn.Pipelines.UnicornSyncBegin;
using Unicorn.Remoting.Serialization;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn.Remoting.Pipelines.UnicornSyncBegin
{
	public class UpdateRemotingItems : IUnicornSyncBeginProcessor
	{
		private readonly DateTime _unsyncedDateTimeValue = new DateTime(1900, 1, 1);

		public void Process(UnicornSyncBeginPipelineArgs args)
		{
			var serializationProvider = args.Configuration.Resolve<ISerializationProvider>();
			var logger = args.Configuration.Resolve<ILogger>();

			var remotingSerializationProvider = serializationProvider as IRemotingSerializationProvider;
			if (remotingSerializationProvider == null) return;

			// get package
			if (string.IsNullOrWhiteSpace(remotingSerializationProvider.RemoteUrl))
			{
				logger.Error("Remoting URL was not set on " + remotingSerializationProvider.GetType().Name + "; cannot update remote.");
				args.AbortPipeline();
				return;
			}

			var lastLoaded = GetLastLoadedTime(args.Configuration.Name);
			var url = string.Format("{0}?c={1}&ts={2}", remotingSerializationProvider.RemoteUrl, args.Configuration.Name, lastLoaded);

			var wc = new WebClient();

			// TODO; add signature to request

			RemotingPackage package = null;

			try
			{
				
				logger.Info("Remoting: Downloading updated items from {0} newer than {1}".FormatWith(remotingSerializationProvider.RemoteUrl, lastLoaded.ToLocalTime()));

				var tempFileName = HostingEnvironment.MapPath("~/temp/" + Guid.NewGuid() + ".zip");

				try
				{
					wc.DownloadFile(url, tempFileName);
					using (var stream = File.OpenRead(tempFileName))
					{
						package = RemotingPackage.FromStream(stream);
					}
				}
				finally
				{
					if (File.Exists(tempFileName)) File.Delete(tempFileName);
				}

				WritePackageToProvider(package, serializationProvider);

				if (package.Manifest.Strategy == RemotingStrategy.Differential)
				{
					// TODO if differential package handle sync with a simple replay so it's fast
					logger.Info("Remoting: received differential package with {0} changes. Replaying instead of sync.".FormatWith(package.Manifest.HistoryEntries.Length));

					//TODO: uncomment once replay works. args.SyncIsHandled = true;
				}
				else
				{
					logger.Info("Remoting: received full package from remote. Deployed and executing sync.");
				}

				SetLastLoadedTime(args.Configuration.Name, package.Manifest.LastSynchronized);
			}
			finally
			{
				// clean up temp files
				if(package != null) package.Dispose();
			}

			// TODO: timestamp last updated management

			args.AbortPipeline(); // TEMP for safety
		}

		protected virtual void WritePackageToProvider(RemotingPackage package, ISerializationProvider provider)
		{
			var sitecoreSerializationProvider = provider as SitecoreSerializationProvider;

			if (sitecoreSerializationProvider == null) throw new InvalidOperationException("I only know how to write to SitecoreSerializationProvider types. Override WritePackageToProvider if you need to do others.");

			var writer = new RemotingPackageWriter(package);

			writer.WriteTo(sitecoreSerializationProvider.SerializationRoot);
		}

		protected virtual DateTime GetLastLoadedTime(string configurationName)
		{
			var db = Factory.GetDatabase("core");
			return db.Properties.GetDateValue("Unicorn_Remoting_" + configurationName, _unsyncedDateTimeValue);
		}

		protected virtual void SetLastLoadedTime(string configurationName, DateTime timestamp)
		{
			var db = Factory.GetDatabase("core");
			db.Properties.SetDateValue("Unicorn_Remoting_" + configurationName, timestamp);
		}
	}
}
