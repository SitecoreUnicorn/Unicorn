using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Unicorn.Configuration;

namespace Unicorn.Pipelines.UnicornReserializeComplete
{
	public class UnicornReserializeCompletePipelineArgs : PipelineArgs
	{
		public UnicornReserializeCompletePipelineArgs(IConfiguration configuration)
		{
			Assert.ArgumentNotNull(configuration, "configuration");

			Configuration = configuration;
		}

		public IConfiguration Configuration { get; private set; }
	}
}
