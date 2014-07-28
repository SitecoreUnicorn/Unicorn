using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;
using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Loader;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.Remoting.Loader
{
	public class RemotingLoader
	{
		private readonly DateTime UnsyncedDateTimeValue = new DateTime(1900, 1, 1);

		public RemotingLoader(ISerializationProvider serializationProvider, ISourceDataProvider sourceDataProvider, IPredicate predicate, IEvaluator evaluator, ISerializationLoaderLogger logger)
		{

		}

		public void LoadAll(string configurationName)
		{
		}


		public DateTime GetLastLoadedTime(string configurationName)
		{
			var db = Factory.GetDatabase("core");
			return db.Properties.GetDateValue("Unicorn_Remoting_" + configurationName, UnsyncedDateTimeValue);
		}

		private void SetLastLoadedTime(string configurationName)
		{
			var db = Factory.GetDatabase("core");
			db.Properties.SetDateValue("Unicorn_Remoting_" + configurationName, DateTime.UtcNow);
		}
	}
}
