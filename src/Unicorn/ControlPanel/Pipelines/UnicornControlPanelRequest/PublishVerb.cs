using System;
using System.Linq;
using System.Web;

using Kamsar.WebConsole;

using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

using Unicorn.ControlPanel.Headings;
using Unicorn.ControlPanel.Responses;
using Unicorn.Logging;
using Unicorn.Publishing;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public class PublishVerb : UnicornControlPanelRequestPipelineProcessor
	{
		public PublishVerb() : base("Publish")
		{
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new WebConsoleResponse("Publish Unicorn Queue", args.SecurityState.IsAutomatedTool, new HeadingService(), progress => Process(progress, new WebConsoleLogger(progress, args.Context.Request.QueryString["log"])));
		}

		protected virtual void Process(IProgressStatus progress, ILogger logger)
		{
			try
			{
				if (ManualPublishQueueHandler.HasItemsToPublish)
				{
					Item trigger = GetTriggerItem();
					Database[] targets = GetTargets();

					Log.Info("Unicorn: initiated synchronous publishing of synced items.", this);
					if (ManualPublishQueueHandler.PublishQueuedItems(trigger, targets, logger))
					{
						Log.Info("Unicorn: publishing of synced items is complete.", this);
					}
				}
				else
				{
					logger.Warn("[Unicorn Publish] There were no items to publish.");
				}
			}
			catch (Exception ex)
			{
				logger.Error(ex);
			}
		}

		protected virtual Item GetTriggerItem()
		{
			Item result;
			string triggerIdOrPath = HttpContext.Current.Request.QueryString["trigger"];
			if (!string.IsNullOrWhiteSpace(triggerIdOrPath))
			{
				result = Factory.GetDatabase("master").GetItem(triggerIdOrPath);
				if (result == null || result.Empty)
				{
					throw new ArgumentException($"No item found for '{triggerIdOrPath}'.", "trigger");
				}
			}
			else
			{
				throw new ArgumentNullException("trigger");
			}

			return result;
		}

		protected virtual Database[] GetTargets()
		{
			Database[] result;
			string targets = HttpContext.Current.Request.QueryString["targets"];
			if (!string.IsNullOrWhiteSpace(targets))
			{
				string[] targetNames = targets.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				result = targetNames.Select(Factory.GetDatabase).ToArray();
				if (result.Length <= 0)
				{
					throw new ArgumentException("At least 1 valid target should be specified.", "targets");
				}
			}
			else
			{
				throw new ArgumentNullException("targets");
			}

			return result;
		}
	}
}
