using Kamsar.WebConsole;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Unicorn.ControlPanel.Headings;
using Unicorn.ControlPanel.Responses;
using Unicorn.Logging;

namespace Unicorn.ControlPanel.Pipelines.UnicornControlPanelRequest
{
	public abstract class GutterVerbBase : UnicornControlPanelRequestPipelineProcessor
	{
		private readonly string _consoleTitle;
		protected const string GutterItemId = "{82496AF6-123F-4724-B7B6-746ED49A7747}";

		protected GutterVerbBase(string verbHandled, string consoleTitle) : base(verbHandled)
		{
			_consoleTitle = consoleTitle;
		}

		protected override IResponse CreateResponse(UnicornControlPanelRequestPipelineArgs args)
		{
			return new WebConsoleResponse(_consoleTitle, args.SecurityState.IsAutomatedTool, new HeadingService(), progress => Process(progress, new WebConsoleLogger(progress, args.Context.Request.QueryString["log"])));
		}

		protected abstract void Process(IProgressStatus process, ILogger logger);

		public static Item GetGutterItem()
		{
			Database coredb = Factory.GetDatabase("core");
			return coredb.DataManager.DataEngine.GetItem(new ID(GutterItemId), LanguageManager.DefaultLanguage, Version.Latest);
		}
	}
}