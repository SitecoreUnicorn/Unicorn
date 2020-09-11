using Sitecore.Pipelines.FilterItem;

namespace Unicorn.ControlPanel
{
	/// <summary>
	/// This is a filterItem pipeline processor that enables arbitrary disabling of item filtering temporarily
	/// This enables a fix to #26 (https://github.com/SitecoreUnicorn/Unicorn/issues/26) when running in live mode
	/// </summary>
	public class CheckIfFilterDisablerIsActive
	{
		public void Process(FilterItemPipelineArgs args)
		{
			if(ItemFilterDisabler.CurrentValue == ItemFilterDisabler.FilterState.Disabled)
				args.AbortPipeline();
		}
	}
}
