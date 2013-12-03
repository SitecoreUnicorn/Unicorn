using System;
using Sitecore.Data;

namespace Unicorn.Data
{
	public interface ISourceItem
	{
		/// <summary>
		/// The name of the item in its tree
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The full path in the hierarchy of the item
		/// </summary>
		string ItemPath { get; }

		/// <summary>
		/// The name of the content database the item resides in
		/// </summary>
		string DatabaseName { get; }

		/// <summary>
		/// The unique ID of the item
		/// </summary>
		ID Id { get; }

		string TemplateName { get; }
		ID TemplateId { get; }

		/// <summary>
		/// The display to use for this item in status updates. Should allow for finding the item easily (e.g. "$database:$fullpath")
		/// </summary>
		string DisplayIdentifier { get; }

		/// <summary>
		/// Recycles or deletes the item
		/// </summary>
		void Recycle();

		/// <summary>
		/// Gets the last modified date for a version of the item
		/// </summary>
		/// <param name="languageCode">The short language code (e.g. "en" or "en-US") of the version</param>
		/// <param name="versionNumber">The number of the version</param>
		/// <returns>The modified date, or null if the version does not exist or has no modified date</returns>
		DateTime? GetLastModifiedDate(string languageCode, int versionNumber);

		/// <summary>
		/// Gets the unique revision ID for a version of the item
		/// </summary>
		/// <param name="languageCode">The short language code (e.g. "en" or "en-US") of the version</param>
		/// <param name="versionNumber">The number of the version</param>
		/// <returns>The revision key, or null if the version does not exist or has no revision value</returns>
		string GetRevision(string languageCode, int versionNumber);

		/// <summary>
		/// Gets the item's child items
		/// </summary>
		ISourceItem[] Children { get; }
	}
}
