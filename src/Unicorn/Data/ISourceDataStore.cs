using System;
using Gibson.Model;

namespace Unicorn.Data
{
	/// <summary>
	/// Gets data items from the source - e.g. Sitecore - for comparison with some serialized items
	/// </summary>
	public interface ISourceDataStore
	{
		/// <summary>
		/// Gets an item from the source data by ID
		/// </summary>
		ISerializableItem GetById(string database, Guid id);

		/// <summary>
		/// Gets an item from the source data by hierarchy path
		/// </summary>
		ISerializableItem GetByPath(string database, string path);

		/// <summary>
		/// Gets children of an item
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		ISerializableItem[] GetChildren(ISerializableItem parent);

		/// <summary>
		/// Recycles (or deletes) an item
		/// </summary>
		/// <param name="item"></param>
		void Recycle(ISerializableItem item);

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
