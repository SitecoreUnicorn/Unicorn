using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Data;
using Unicorn.Predicates;

namespace Unicorn.Serialization.Sitecore
{
	public class SitecoreSerializationProvider : ISerializationProvider
	{
		private readonly string _rootPath;
		private readonly string _logName;
		private readonly Prefetcher _prefetcher;
		private readonly IPredicate _predicate;

		public SitecoreSerializationProvider()
			: this(PathUtils.Root, "UnicornItemSerialization", new SerializationPresetPredicate(new SitecoreSourceDataProvider()))
		{

		}

		public SitecoreSerializationProvider(string rootPath, string logName, IPredicate predicate)
		{
			_predicate = predicate;
			_rootPath = rootPath;
			_logName = logName;
			_prefetcher = new Prefetcher(ReadItemFromDisk);
		}

		public virtual string LogName
		{
			get { return _logName; }
		}

		public virtual ISerializedItem SerializeItem(ISourceItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			var sitecoreSourceItem = item as SitecoreSourceItem;

			var sitecoreItem = sitecoreSourceItem != null ? sitecoreSourceItem.InnerItem : Factory.GetDatabase(item.DatabaseName).GetItem(item.Id);

			Assert.IsNotNull(sitecoreItem, "Item to serialize did not exist!");

			var serializedPath = SerializationPathUtility.GetSerializedItemPath(_rootPath, item);

			var serializedItem = new SitecoreSerializedItem(ItemSynchronization.BuildSyncItem(sitecoreItem), serializedPath);

			UpdateSerializedItem(serializedItem);

			return serializedItem;
		}

		public virtual ISerializedReference GetReference(ISourceItem sourceItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			var prefetched = _prefetcher.GetItem(sourceItem.Id);
			if (prefetched != null) return prefetched;

			var physicalPath = SerializationPathUtility.GetSerializedReferencePath(_rootPath, sourceItem);

			if (!Directory.Exists(physicalPath))
			{
				physicalPath = SerializationPathUtility.GetSerializedItemPath(_rootPath, sourceItem);

				if (!File.Exists(physicalPath))
					return null;
			}

			return new SitecoreSerializedReference(physicalPath);
		}

		public virtual ISerializedReference[] GetChildReferences(ISerializedReference parent, bool recursive)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var longPath = SerializationPathUtility.GetReferenceDirectoryPath(parent);
			var shortPath = SerializationPathUtility.GetShortSerializedReferencePath(_rootPath, parent);

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

		public virtual ISerializedItem GetItem(ISerializedReference reference)
		{
			Assert.ArgumentNotNull(reference, "reference");

			var prefetched = _prefetcher.GetItem(sourceItem.Id);
			if (prefetched != null) return prefetched;

			var path = SerializationPathUtility.GetReferenceItemPath(reference);

			if (!File.Exists(path)) return null;

			return ReadItemFromDisk(path);
		}

		public virtual ISerializedItem[] GetChildItems(ISerializedReference parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var path = SerializationPathUtility.GetReferenceDirectoryPath(parent);
			if (!Directory.Exists(path)) return new ISerializedItem[0];

			string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);

			return files.Select(ReadItemFromDisk).ToArray();
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

		public virtual ISourceItem DeserializeItem(ISerializedItem serializedItem)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			try
			{
				var options = new LoadOptions { DisableEvents = true, ForceUpdate = true, UseNewID = false };

				return new SitecoreSourceItem(ItemSynchronization.PasteSyncItem(typed.InnerItem, options, true));
			}
			catch (ParentItemNotFoundException ex)
			{
				string error = "Cannot load item from path '{0}'. Probable reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex.ParentID);

				throw new DeserializationException(error, ex);
			}
			catch (ParentForMovedItemNotFoundException ex2)
			{
				string error = "Item from path '{0}' cannot be moved to appropriate location. Possible reason: parent item with ID '{1}' not found.".FormatWith(serializedItem.ProviderId, ex2.ParentID);

				throw new DeserializationException(error, ex2);
			}
		}

		public virtual void UpdateSerializedItem(ISerializedItem serializedItem)
		{
			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			// create any requisite parent folder(s) for the serialized item
			var parentPath = Directory.GetParent(SerializationPathUtility.GetReferenceDirectoryPath(serializedItem));
			if (parentPath != null && !parentPath.Exists)
				Directory.CreateDirectory(parentPath.FullName);

			using (var fileStream = File.Open(serializedItem.ProviderId, FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				using (var writer = new StreamWriter(fileStream))
				{
					typed.InnerItem.Serialize(writer);
				}
			}
		}

		public virtual void RenameSerializedItem(ISourceItem renamedItem, string oldName)
		{
			if (renamedItem == null || oldName == null) return;

			var typed = renamedItem as SitecoreSourceItem;

			if (typed == null) throw new ArgumentException("Renamed item must be a SitecoreSourceItem", "renamedItem");

			// write the serialized item under its new name
			var updatedItem = SerializeItem(renamedItem);

			// find the children directory path of the previous item name, if it exists, and move them to the new child path
			var renamedParentSerializationDirectory = Directory.GetParent(SerializationPathUtility.GetSerializedReferencePath(_rootPath, renamedItem));

			var oldSerializedChildrenReference = new SitecoreSerializedReference(renamedParentSerializationDirectory.FullName + Path.DirectorySeparatorChar + oldName);

			if (Directory.Exists(oldSerializedChildrenReference.ProviderId))
				MoveDescendants(oldSerializedChildrenReference, updatedItem);

			// delete the original serialized item from pre-rename
			DeleteSerializedItem(new SitecoreSerializedReference(SerializationPathUtility.GetReferenceItemPath(oldSerializedChildrenReference)));
		}

		public virtual void MoveSerializedItem(ISourceItem sourceItem, ISourceItem newParentItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(newParentItem, "newParentItem");

			var sitecoreSource = sourceItem as SitecoreSourceItem;
			var sitecoreParent = newParentItem as SitecoreSourceItem;

			if (sitecoreParent == null) throw new ArgumentException("newParentItem must be a SitecoreSourceItem", "newParentItem");
			if (sitecoreSource == null) throw new ArgumentException("sourceItem must be a SitecoreSourceItem", "sourceItem");

			var oldRootDirectory = new SitecoreSerializedReference(SerializationPathUtility.GetSerializedReferencePath(_rootPath, sourceItem));
			var oldRootItemPath = new SitecoreSerializedReference(SerializationPathUtility.GetReferenceItemPath(oldRootDirectory));
			var newRootItemPath = SerializationPathUtility.GetSerializedReferencePath(_rootPath, newParentItem) + Path.DirectorySeparatorChar + sourceItem.Name + PathUtils.Extension;

			var syncItem = ItemSynchronization.BuildSyncItem(sitecoreSource.InnerItem);

			// update the path and parent IDs to the new location
			syncItem.ParentID = newParentItem.Id.ToString();
			syncItem.ItemPath = string.Concat(newParentItem.ItemPath, "/", syncItem.Name);

			var serializedNewItem = new SitecoreSerializedItem(syncItem, newRootItemPath);

			// write the moved sync item to its new destination
			UpdateSerializedItem(serializedNewItem);

			// move any children to the new destination (and fix their paths)
			MoveDescendants(oldRootDirectory, serializedNewItem);

			// remove the serialized item in the old location
			DeleteSerializedItem(oldRootItemPath);
		}

		public virtual void DeleteSerializedItem(ISerializedReference item)
		{
			// kill the serialized file
			if (File.Exists(item.ProviderId)) File.Delete(item.ProviderId);
			else return;

			// remove any serialized children
			var directory = SerializationPathUtility.GetReferenceDirectoryPath(item);

			if (Directory.Exists(directory)) Directory.Delete(directory, true);

			// clean up empty parent folder(s)
			var parentDirectory = Directory.GetParent(directory);

			do
			{
				if (parentDirectory.GetFileSystemInfos().Length > 0) break;

				parentDirectory.Delete(true);
				parentDirectory = parentDirectory.Parent;

			} while (parentDirectory != null && parentDirectory.Exists);
		}

		protected virtual ISerializedItem ReadItemFromDisk(string fullPath)
		{
			using (var fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var reader = new StreamReader(fileStream))
				{
					var item = SyncItem.ReadItem(new Tokenizer(reader), true);

					Assert.IsNotNullOrEmpty(item.TemplateID, "{0}: TemplateID was not valid!", fullPath);

					return new SitecoreSerializedItem(item, fullPath);
				}
			}
		}

		protected virtual void MoveDescendants(ISerializedReference oldReference, ISerializedItem newItem)
		{
			// TODO: it would make more sense to, instead of copying existing serialized children around,
			// TODO: to instead get all descendants in Sitecore that are included by the predicate and serialize any that are included.
			// TODO: this is complicated because MoveItem receives an item *with the old path* - so it becomes weird to calculate inclusion when the move is still incomplete
			// TODO: however, it's a better experience so it should be implemented at some point. RenameItem recieves an already renamed item, so its path ahead is obvious.

			// remove the extension from the new item's provider ID
			string newItemReferencePath = SerializationPathUtility.GetReferenceDirectoryPath(newItem);

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
			if (Directory.Exists(oldReference.ProviderId)) Directory.Delete(oldReference.ProviderId, true);
		}
	}
}
