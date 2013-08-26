using System.Linq;
using Sitecore.Data;

namespace Unicorn.Data
{
	public interface ISourceDataProvider
	{
		ISourceItem GetItem(string database, ID id);
		void ResetTemplateEngine();

		/// <summary>
		/// Sends a deserialization completed message to the provider (e.g. loading is complete)
		/// </summary>
		/// <param name="databaseName">The name of the database that was loaded to</param>
		void DeserializationComplete(string databaseName);
	}

}
