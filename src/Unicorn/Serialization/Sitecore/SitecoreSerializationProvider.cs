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
		public ISerializedItem SerializeItem(ISourceItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			var sitecoreSourceItem = item as SitecoreSourceItem;

			var sitecoreItem = sitecoreSourceItem != null ? sitecoreSourceItem.InnerItem : Factory.GetDatabase(item.Database).GetItem(item.Id);

			Assert.IsNotNull(sitecoreItem, "Item to serialize did not exist!");

			var serializedPath = PathUtils.GetFilePath(new ItemReference(sitecoreItem).ToString());
			var serializedItem = new SitecoreSerializedItem(ItemSynchronization.BuildSyncItem(sitecoreItem), serializedPath);

			UpdateSerializedItem(serializedItem);

			return serializedItem;
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

				if (!File.Exists(physicalPath))
					return null;
			}

			return new SitecoreSerializedReference(physicalPath);
		}

		public ISerializedReference[] GetChildReferences(ISerializedReference parent, bool recursive)
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

			List<ISerializedReference> referenceResults = results.Select(x => (ISerializedReference)new SitecoreSerializedReference(x)).ToList();

			if (recursive)
			{
				var localReferenceResults = referenceResults.ToArray();
				foreach (var child in localReferenceResults)
				{
					referenceResults.AddRange(GetChildReferences(child, true));
				}
			}

			return referenceResults.ToArray();
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

			var updatedItem = SerializeItem(renamedItem);

			var reference = new ItemReference(typed.InnerItem).ToString();
			var oldReference = reference.Substring(0, reference.LastIndexOf('/') + 1) + oldName;

			var oldSerializationPath = PathUtils.GetDirectoryPath(oldReference);

			if (Directory.Exists(oldSerializationPath))
				MoveDescendants(new SitecoreSerializedReference(oldSerializationPath), updatedItem);
		}

		public void MoveSerializedItem(ISourceItem sourceItem, ISourceItem newParentItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(newParentItem, "newParentItem");

			var sitecoreSource = sourceItem as SitecoreSourceItem;
			var sitecoreParent = newParentItem as SitecoreSourceItem;

			if (sitecoreParent == null) throw new ArgumentException("newParentItem must be a SitecoreSourceItem", "newParentItem");
			if (sitecoreSource == null) throw new ArgumentException("sourceItem must be a SitecoreSourceItem", "sourceItem");

			var oldRootDirectory = new SitecoreSerializedReference(PathUtils.GetDirectoryPath(new ItemReference(sitecoreSource.InnerItem).ToString()));
			var oldRootItemPath = new SitecoreSerializedReference(oldRootDirectory.ProviderId + PathUtils.Extension);
			var newRootItemPath = PathUtils.GetDirectoryPath(new ItemReference(sitecoreParent.InnerItem).ToString()) + "/" + sitecoreSource.Name + PathUtils.Extension;

			var syncItem = ItemSynchronization.BuildSyncItem(sitecoreSource.InnerItem);

			// update the path and parent IDs to the new location
			syncItem.ParentID = newParentItem.Id.ToString();
			syncItem.ItemPath = string.Concat(newParentItem.Path, "/", syncItem.Name);

			var serializedNewItem = new SitecoreSerializedItem(syncItem, newRootItemPath);

			// write the moved sync item to its new destination
			UpdateSerializedItem(serializedNewItem);

			// move any children to the new destination (and fix their paths)
			MoveDescendants(oldRootDirectory, serializedNewItem);

			// remove the serialized item in the old location
			DeleteSerializedItem(oldRootItemPath);
		}

		public void CopySerializedItem(ISourceItem sourceItem, ISourceItem destination)
		{
			throw new NotImplementedException();
		}

		public void DeleteSerializedItem(ISerializedReference item)
		{
			// kill the serialized file
			if (File.Exists(item.ProviderId)) File.Delete(item.ProviderId);
			else return;

			// remove any serialized children
			var directory = PathUtils.StripPath(item.ProviderId);
			
			if(Directory.Exists(directory)) Directory.Delete(directory, true);

			// clean up empty parent folder(s)
			var parentDirectory = Directory.GetParent(directory);

			do
			{
				if (parentDirectory.GetFileSystemInfos().Length > 0) break;

				parentDirectory.Delete(true);
				parentDirectory = parentDirectory.Parent;

			} while (parentDirectory != null && parentDirectory.Exists);
		}

		private void MoveDescendants(ISerializedReference oldReference, ISerializedItem newItem)
		{
			// remove the extension from the new item's provider ID
			string newItemReferencePath = PathUtils.StripPath(newItem.ProviderId);

			// if the paths were the same, no moving occurs (this can happen when saving templates, which spuriously can report "renamed" when they are not actually any such thing)
			if (oldReference.ProviderId.Equals(newItemReferencePath, StringComparison.OrdinalIgnoreCase)) return;

			var serializedDescendants = GetChildReferences(oldReference, true);

			// take all the descendant items that are serialized already, and re-serialize them at their new path location
			foreach (var descendant in serializedDescendants)
			{
				var item = GetItem(descendant) as SitecoreSerializedItem;

				if (item == null) continue;

				var syncItem = item.InnerItem;

				syncItem.ItemPath = syncItem.ItemPath.Replace(oldReference.ItemPath, newItem.ItemPath);
				var newPhysicalPath = item.ProviderId.Replace(oldReference.ProviderId, newItemReferencePath);

				UpdateSerializedItem(new SitecoreSerializedItem(syncItem, newPhysicalPath));
			}

			// remove the old children folder if it exists
			if(Directory.Exists(oldReference.ProviderId)) Directory.Delete(oldReference.ProviderId, true);
		}
	}
}
