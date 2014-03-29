using Sitecore.Data;

namespace Unicorn.Data
{
	/// <summary>
	/// Gets data items from the source - e.g. Sitecore - for comparison with some serialized items
	/// </summary>
	public interface ISourceDataProvider
	{
		/// <summary>
		/// Gets an item from the source data by ID
		/// </summary>
		ISourceItem GetItemById(string database, ID id);

		/// <summary>
		/// Gets an item from the source data by hierarchy path
		/// </summary>
		ISourceItem GetItemByPath(string database, string path);

		/// <summary>
		/// Signals the provider to clear its template engine after a template item has been modified.
		/// </summary>
		void ResetTemplateEngine();

		/// <summary>
		/// Sends a deserialization completed message to the provider (e.g. loading is complete)
		/// </summary>
		/// <param name="databaseName">The name of the database that was loaded to</param>
		void DeserializationComplete(string databaseName);
	}
}
