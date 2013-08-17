using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;

namespace Unicorn.Data
{
	public class SitecoreSourceDataProvider : ISourceDataProvider
	{
		public void ResetTemplateEngine()
		{
			foreach (Database current in Factory.GetDatabases())
			{
				current.Engines.TemplateEngine.Reset();
			}
		}

		public ISourceItem GetItem(string database, ID id)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(id, "id");

			Database db = Factory.GetDatabase(database);

			Assert.IsNotNull(db, "Database " + database + " did not exist!");

			return new SitecoreSourceItem(db.GetItem(id));
		}
	}
}
