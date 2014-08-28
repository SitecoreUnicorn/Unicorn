using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;
using Unicorn.Predicates;
using Unicorn.Serialization.Sitecore.Fiat;

namespace Unicorn.Remoting.Serialization
{
	public class FiatRemotedSerializationProvider : FiatSitecoreSerializationProvider, IRemotingSerializationProvider
	{
		public FiatRemotedSerializationProvider(IPredicate predicate, IFieldPredicate fieldPredicate, IFiatDeserializerLogger logger, string remoteUrl = null, string rootPath = null, string logName = "UnicornItemSerialization")
			: base(predicate, fieldPredicate, logger, rootPath, logName)
		{
			Assert.ArgumentNotNull(remoteUrl, "remoteUrl");

			RemoteUrl = remoteUrl;
		}

		public string RemoteUrl { get; private set; }
		public bool DisableDifferentialSync { get; set; }

		public override string FriendlyName
		{
			get { return "Fiat Remoted Serialization Provider"; }
		}

		public override string Description
		{
			get { return "Pulls content from a specified remote Sitecore instance and syncs it into this one as the normal Fiat provider would."; }
		}

		public override KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return base.GetConfigurationDetails().Concat(new[] { new KeyValuePair<string, string>("Remote URL", RemoteUrl) }).ToArray();
		}
	}
}
