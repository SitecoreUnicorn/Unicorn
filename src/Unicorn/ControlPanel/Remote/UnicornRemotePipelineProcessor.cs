using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Sitecore.Configuration;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.ControlPanel.Remote.Logging;
using Unicorn.ControlPanel.Security;
using Unicorn.Data;
using Unicorn.Logging;
using Unicorn.Predicates;

namespace Unicorn.ControlPanel.Remote
{
	public class UnicornRemotePipelineProcessor : UnicornControlPanelPipelineProcessor
	{
		protected static readonly string CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
		private static readonly IUnicornAuthenticationProvider AuthenticationProvider = (IUnicornAuthenticationProvider)Factory.CreateObject("/sitecore/unicorn/authenticationProvider", true);

		public UnicornRemotePipelineProcessor(string activationUrl) : base(activationUrl)
		{
		}

		public override void ProcessRequest(HttpContext context)
		{
			context.Server.ScriptTimeout = 86400;

			var verb = (context.Request.QueryString["verb"] ?? string.Empty).ToLowerInvariant();

			if (verb == "Challenge")
			{
				context.Response.ContentType = "text/plain";
				context.Response.Write(AuthenticationProvider.GetChallengeToken());
				context.Response.End();
				return;
			}

			if (!Authorization.IsAllowed)
			{
				context.Response.StatusCode = 401;
				context.Response.StatusDescription = "Not Authorized";
			}
			else
			{
				switch (verb)
				{
					case "sync":
						Process(context, ProcessSync);
						break;
					case "reserialize":
						Process(context, ProcessReserialize);
						break;
					case "handshake":
						SetSuccessResponse(context);
						break;
					case "config":
						ProcessConfiguration(context);
						break;
					default:
						SetResponse(context, 404, "Not Found");
						break;
				}
			}
		}

		protected virtual void Process(HttpContext context, Action<RemoteLogger> action)
		{
			context.Response.Buffer = false;
			context.Response.BufferOutput = false;
			context.Response.ContentType = "text/plain";
			SetSuccessResponse(context);
			using (var outputStream = context.Response.OutputStream)
			{
				using (var streamWriter = new StreamWriter(outputStream))
				{
					var progress = new RemoteLogger(streamWriter);
					using (new UnicornOperationContext())
					{
						action(progress);
					}
				}
			}
		}

		protected virtual void ProcessSync(RemoteLogger progress)
		{
			foreach (var configuration in ResolveConfigurations())
			{
				var logger = configuration.Resolve<ILogger>();
				var helper = configuration.Resolve<SerializationHelper>();

				using (new LoggingContext(progress, configuration))
				{
					try
					{
						logger.Info("Remote Sync: Processing Unicorn configuration " + configuration.Name);

						using (new TransparentSyncDisabler())
						{
							var pathResolver = configuration.Resolve<PredicateRootPathResolver>();

							var roots = pathResolver.GetRootSerializedItems();

							var index = 0;

							helper.SyncTree(configuration, item =>
							{
								progress.ReportProgress((int)(((index + 1) / (double)roots.Length) * 100));
								index++;
							}, roots);
						}

						logger.Info("Remote Sync: Completed syncing Unicorn configuration " + configuration.Name);
					}
					catch (Exception ex)
					{
						logger.Error(ex);
						break;
					}
				}
			}
		}

		protected virtual void ProcessReserialize(RemoteLogger progress)
		{
			foreach (var configuration in ResolveConfigurations())
			{
				var logger = configuration.Resolve<ILogger>();
				using (new LoggingContext(progress, configuration))
				{
					try
					{
						var timer = new Stopwatch();
						timer.Start();

						logger.Info(configuration.Name + " is being reserialized");

						using (new TransparentSyncDisabler())
						{
							var targetDataStore = configuration.Resolve<ITargetDataStore>();
							var helper = configuration.Resolve<SerializationHelper>();

							// nuke any existing items in the store before we begin. This is a full reserialize so we want to
							// get rid of any existing stuff even if it's not part of existing configs
							logger.Warn("[D] Clearing existing items from {0}".FormatWith(targetDataStore.FriendlyName));
							targetDataStore.Clear();

							var roots = configuration.Resolve<PredicateRootPathResolver>().GetRootSourceItems();

							int index = 1;
							foreach (var root in roots)
							{
								helper.DumpTree(root, configuration);
								progress.ReportProgress((int)((index / (double)roots.Length) * 100));
								index++;
							}
						}

						timer.Stop();

						logger.Info("{0} reserialization complete in {1}ms".FormatWith(configuration.Name, timer.ElapsedMilliseconds));
					}
					catch (Exception ex)
					{
						logger.Error(ex);
						break;
					}
				}
			}
		}

		protected virtual IConfiguration[] ResolveConfigurations()
		{
			var config = HttpContext.Current.Request.QueryString["configuration"];
			var targetConfigurations = ControlPanelUtility.ResolveConfigurationsFromQueryParameter(config);

			if (targetConfigurations.Length == 0) throw new ArgumentException("Configuration(s) requested were not defined.");

			return targetConfigurations;
		}

		protected virtual void ProcessConfiguration(HttpContext context)
		{
			var configs = UnicornConfigurationManager.Configurations.Select(c => c.Name);
			var configsString = string.Join(",", configs);

			SetTextResponse(context, configsString);
		}

		protected virtual void SetTextResponse(HttpContext context, string data)
		{
			SetSuccessResponse(context);
			context.Response.AddHeader("Content-Type", "text/plain");
			context.Response.Output.Write(data);
		}

		protected virtual void SetSuccessResponse(HttpContext context)
		{
			SetResponse(context, 200, "OK");
		}

		protected virtual void SetResponse(HttpContext context, int statusCode, string description)
		{
			context.Response.StatusCode = statusCode;
			context.Response.StatusDescription = description;
			context.Response.AddHeader("X-Remote-Version", CurrentVersion);
		}
	}
}