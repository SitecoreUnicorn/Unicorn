using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Configuration;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;

namespace Unicorn.Serialization.Sitecore
{
	public class SitecoreSerializationProvider : ISerializationProvider
	{
		// TODO: should have some kind of global write lock (across serialize item and updateserializeditem)

		public void SerializeItem(ISourceItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			var sitecoreSourceItem = item as SitecoreSourceItem;

			var sitecoreItem = sitecoreSourceItem != null ? sitecoreSourceItem.InnerItem : Factory.GetDatabase(item.Database).GetItem(item.Id);

			Assert.IsNotNull(sitecoreItem, "Item to dump did not exist!");

			Manager.DumpItem(sitecoreItem);
			Manager.CleanupPath(PathUtils.GetDirectoryPath(new ItemReference(sitecoreItem.Parent).ToString()), false);
		}

		public ISerializedReference GetReference(string sitecorePath, string databaseName)
		{
			Assert.ArgumentNotNullOrEmpty(sitecorePath, "sitecorePath");
			Assert.ArgumentNotNullOrEmpty(databaseName, "databaseName");

			var reference = new ItemReference(databaseName, sitecorePath);

			var physicalPath = PathUtils.GetDirectoryPath(reference.ToString());

			if (!Directory.Exists(physicalPath))
			{
				physicalPath = PathUtils.GetFilePath(reference.ToString());

				if(!File.Exists(physicalPath))
					throw new FileNotFoundException("The reference path " + physicalPath + " did not exist!", physicalPath);
			}

			return new SitecoreSerializedReference(physicalPath);
		}

		public ISerializedReference[] GetChildReferences(ISerializedReference parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var longPath = PathUtils.StripPath(parent.ProviderId);
			var shortPath = PathUtils.GetShortPath(longPath);

			Func<string, string[]> parseDirectory = path =>
				{
					if (Directory.Exists(path))
					{
						var resultSet = new HashSet<string>();

						string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);

						foreach (var file in files)
							resultSet.Add(file);

						string[] directories = PathUtils.GetDirectories(path);

						// add directories that aren't already ref'd indirectly by a file
						foreach (var directory in directories)
						{
							if (CommonUtils.IsDirectoryHidden(directory)) continue;
							
							if (!resultSet.Contains(directory + PathUtils.Extension))
								resultSet.Add(directory);
						}

						string[] resultArray = resultSet.ToArray();

						// make sure if a "templates" item exists in the current set, it goes first
						if (resultArray.Length > 1)
						{
							for (int i = 1; i < resultArray.Length; i++)
							{
								if ("templates".Equals(Path.GetFileName(resultArray[i]), StringComparison.OrdinalIgnoreCase))
								{
									string text = resultArray[0];
									resultArray[0] = resultArray[i];
									resultArray[i] = text;
								}
							}
						}

						return resultArray;
					}

					return new string[0];
				};

			var results = Enumerable.Concat(parseDirectory(longPath), parseDirectory(shortPath));

			return results.Select(x => (ISerializedReference)new SitecoreSerializedReference(x)).ToArray();
		}

		public ISerializedItem GetItem(ISerializedReference reference)
		{
			Assert.ArgumentNotNull(reference, "reference");

			var path = reference.ProviderId.EndsWith(PathUtils.Extension)
							? reference.ProviderId
							: reference.ProviderId + PathUtils.Extension;

			if (!File.Exists(path)) return null;

			using (var reader = new StreamReader(path))
			{
				var syncItem = SyncItem.ReadItem(new Tokenizer(reader), true);

				return new SitecoreSerializedItem(syncItem, path);
			}
		}

		public ISerializedItem[] GetChildItems(ISerializedReference parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var path = PathUtils.StripPath(parent.ProviderId);
			if (!Directory.Exists(path)) return new ISerializedItem[0];

			var results = new List<ISerializedItem>();

			string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);
			foreach (string fileName in files)
			{
				using (var reader = new StreamReader(fileName))
				{
					var syncItem = SyncItem.ReadItem(new Tokenizer(reader), true);

					results.Add(new SitecoreSerializedItem(syncItem, fileName));
				}
			}

			return results.ToArray();
		}

		public bool IsStandardValuesItem(ISerializedItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			string[] array = item.ItemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length > 0)
			{
				if (array.Any(s => s.Equals("templates", StringComparison.OrdinalIgnoreCase)))
				{
					return array.Last().Equals("__Standard Values", StringComparison.OrdinalIgnoreCase);
				}
			}

			return false;
		}

		public ISourceItem DeserializeItem(ISerializedItem serializedItem, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(progress, "progress");

			
			var typed = serializedItem as SitecoreSerializedItem;

			if(typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			try
			{
				var options = new LoadOptions { DisableEvents = true, ForceUpdate = true, UseNewID = false };

				return new SitecoreSourceItem(ItemSynchronization.PasteSyncItem(typed.InnerItem, options, true));
			}
			catch (ParentItemNotFoundException ex)
			{
				string error = "Cannot load item from path '{0}'. Probable reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex.ParentID);

				progress.ReportStatus(error, MessageType.Error);

				return null;
			}
			catch (ParentForMovedItemNotFoundException ex2)
			{
				string error = "Item from path '{0}' cannot be moved to appropriate location. Possible reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex2.ParentID);

				progress.ReportStatus(error, MessageType.Error);

				return null;
			}
		}

		public void UpdateSerializedItem(ISerializedItem serializedItem)
		{
			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			var parentPath = Path.GetDirectoryName(serializedItem.ProviderId);
			if (parentPath != null)
				Directory.CreateDirectory(parentPath);

			using (var fileStream = File.Open(serializedItem.ProviderId, FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				using (var writer = new StreamWriter(fileStream))
				{
					typed.InnerItem.Serialize(writer);
				}
			}
		}

		public void RenameSerializedItem(ISourceItem renamedItem, string oldName)
		{
			if (renamedItem == null || oldName == null) return;

			var typed = renamedItem as SitecoreSourceItem;

			if (typed == null) throw new ArgumentException("Renamed item must be a SitecoreSourceItem", "renamedItem");

			// the name wasn't actually changed, you sneaky template builder you. Don't write.
			if (oldName.Equals(renamedItem.Name, StringComparison.Ordinal)) return;

			// we push this to get updated. Because saving now ignores "inconsquential" changes like a rename that do not change data fields,
			// this keeps renames occurring even if the field changes are inconsequential
			SerializeItem(renamedItem);

			var reference = new ItemReference(typed.InnerItem).ToString();
			var oldReference = reference.Substring(0, reference.LastIndexOf('/') + 1) + oldName;

			var oldSerializationPath = PathUtils.GetDirectoryPath(oldReference);
			var newSerializationPath = PathUtils.GetDirectoryPath(reference);

			if (Directory.Exists(oldSerializationPath))
				MoveDescendants(oldSerializationPath, newSerializationPath, renamedItem.Database);
		}

		public void MoveSerializedItem(ISourceItem sourceItem, global::Sitecore.Data.ID newParentId)
		{
			throw new NotImplementedException();
		}

		public void CopySerializedItem(ISourceItem sourceItem, ISourceItem destination)
		{
			throw new NotImplementedException();
		}

		public void DeleteSerializedItem(ISerializedItem item)
		{
			throw new NotImplementedException();
		}

		private void MoveDescendants(string oldSitecorePath, string newSitecorePath, string databaseName)
		{
			// if the paths were the same, no moving occurs (this can happen when saving templates, which spuriously can report "renamed" when they are not actually any such thing)
			if (oldSitecorePath.Equals(newSitecorePath, StringComparison.OrdinalIgnoreCase)) return;

			//var oldSerializationPath = GetPhysicalPath(new ItemReference(databaseName, oldSitecorePath));

			//// move descendant items by reserializing them and fixing their ItemPath
			//if (descendantItems.Length > 0)
			//{
			//	foreach (var descendant in descendantItems)
			//	{
			//		string oldPath = GetPhysicalSyncItemPath(descendant);

			//		// save to new location
			//		descendant.ItemPath = descendant.ItemPath.Replace(oldSitecorePath, newSitecorePath);
			//		SaveItem(descendant);

			//		// remove old file location
			//		if (File.Exists(oldPath))
			//		{
			//			using (new WatcherDisabler(_watcher))
			//			{
			//				File.Delete(oldPath);
			//			}
			//		}
			//	}
			//}

			//using (new WatcherDisabler(_watcher))
			//{
			//	// remove the old serialized item from disk
			//	if (File.Exists(oldSerializationPath)) File.Delete(oldSerializationPath);

			//	// remove the old serialized children folder from disk
			//	var directoryPath = PathUtils.StripPath(oldSerializationPath);
			//	if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, true);
			//}
		}
	}
}
