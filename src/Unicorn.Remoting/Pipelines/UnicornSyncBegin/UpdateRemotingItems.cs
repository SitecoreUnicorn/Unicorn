using System;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Hosting;
using Sitecore.Configuration;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
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

			if (HttpContext.Current != null && HttpContext.Current.Request.Url.Host.Equals(new Uri(remotingSerializationProvider.RemoteUrl).Host, StringComparison.OrdinalIgnoreCase))
			{
				logger.Warn("Remoting: Remote URL was local instance - skipping this configuration as it is by definition already synced.");
				args.SyncIsHandled = true;
				return;
			}

			var lastLoaded = GetLastLoadedTime(args.Configuration.Name);

			// if you pass the force parameter, we do not use the history engine differential sync
			if (remotingSerializationProvider.DisableDifferentialSync || (HttpContext.Current != null && HttpContext.Current.Request.QueryString["force"] != null))
			{
				lastLoaded = _unsyncedDateTimeValue;
			}

			var url = string.Format("{0}?c={1}&ts={2}", remotingSerializationProvider.RemoteUrl, args.Configuration.Name, lastLoaded);

			var webClient = new SuperWebClient();

			// TODO; add signature to request

			RemotingPackage package = null;

			try
			{
				logger.Info("Remoting: Downloading updated items from {0} newer than {1}".FormatWith(remotingSerializationProvider.RemoteUrl, lastLoaded.ToLocalTime()));

				var tempFileName = HostingEnvironment.MapPath("~/temp/" + Guid.NewGuid() + ".zip");

				try
				{
					webClient.DownloadFile(url, tempFileName);
					using (var stream = File.OpenRead(tempFileName))
					{
						package = RemotingPackage.FromStream(stream);
					}
				}
				finally
				{
					if (File.Exists(tempFileName)) File.Delete(tempFileName);
				}

				WritePackageToProvider(package, args.Configuration);

				if (package.Manifest.Strategy == RemotingStrategy.Differential)
				{
					logger.Info("Remoting: received differential package with {0} changes. Replaying instead of sync.".FormatWith(package.Manifest.HistoryEntries.Length));

					var replayer = new DifferentialPackageReplayer(package);

					if (!replayer.Replay(logger))
					{
						logger.Error("Remoting package replay signalled an error. Aborting.");
						args.AbortPipeline();
						return;
					}
					else args.SyncIsHandled = true;
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
		}

		protected virtual void WritePackageToProvider(RemotingPackage package, IConfiguration configuration)
		{
			var sitecoreSerializationProvider = configuration.Resolve<ISerializationProvider>() as SitecoreSerializationProvider;

			if (sitecoreSerializationProvider == null) throw new InvalidOperationException("I only know how to write to SitecoreSerializationProvider types. Override WritePackageToProvider if you need to do others.");

			var writer = configuration.Resolve<RemotingPackageWriter>();

			writer.WriteTo(package, sitecoreSerializationProvider.SerializationRoot);
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

		private class SuperWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri uri)
			{
				WebRequest w = base.GetWebRequest(uri);
				w.Timeout = 20 * 60 * 1000;
				return w;
			}
		}
	}
}
