using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sitecore.Configuration;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Unicorn.ControlPanel;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization.Sitecore.Formatting;

namespace Unicorn.Serialization.Sitecore
{
	/// <summary>
	/// Serializes and deserializes items using the standard Sitecore serialization APIs.
	/// NOTE: this does not support FieldPredicates. You should probably use FiatSitecoreSerializationProvider instead of this.
	/// </summary>
	public class SitecoreSerializationProvider : ISerializationProvider, IDocumentable
	{
		private readonly string _rootPath;
		private readonly string _logName;
		private readonly IPredicate _predicate;
		private readonly ISitecoreSerializationFormatter _formatter;

		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="formatter">The formatter defines the format in which serialized items are written to disk</param>
		/// <param name="rootPath">The root serialization path to write files to. Defaults to PathUtils.RootPath if the default value (null) is passed.</param>
		/// <param name="logName">The prefix to write log entries with. Useful if you have multiple serialization providers.</param>
		/// <param name="predicate">The predicate to use. If null, uses Registry to look up the registered DI instance.</param>
		public SitecoreSerializationProvider(IPredicate predicate, ISitecoreSerializationFormatter formatter, string rootPath = null, string logName = "UnicornItemSerialization")
		{
			rootPath = (rootPath == null || rootPath == "default") ? PathUtils.Root : rootPath;

			// allow root-relative serialization path (e.g. ~/data/serialization or ~/../data/serialization)
			rootPath = ConfigurationUtility.ResolveConfigurationPath(rootPath);

			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");
			Assert.ArgumentNotNullOrEmpty(logName, "logName");
			Assert.ArgumentNotNull(predicate, "predicate");

			_predicate = predicate;
			_formatter = formatter;

			// an unspoken expectation of the Sitecore path utils is that the serialization root is always post-fixed with the directory separator char
			// if this is not the case, custom path resolution can result in weird sitecore path mappings
			if (rootPath[rootPath.Length - 1] != Path.DirectorySeparatorChar)
				rootPath += Path.DirectorySeparatorChar;

			_rootPath = rootPath;
			_logName = logName;
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


			var serializedPath = GetSerializedItemPath(_rootPath, item);

			var serializedItem = new SitecoreSerializedItem(ItemSynchronization.BuildSyncItem(sitecoreItem), serializedPath, this);

			UpdateSerializedItem(serializedItem);

			return serializedItem;
		}

		public virtual ISerializedReference GetReference(ISourceItem sourceItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");

			var physicalPath = GetSerializedReferencePath(_rootPath, sourceItem);

			if (!Directory.Exists(physicalPath))
			{
				physicalPath = GetSerializedItemPath(_rootPath, sourceItem);

				if (!File.Exists(physicalPath))
					return null;
			}

			return new SitecoreSerializedReference(physicalPath, this);
		}

		public virtual ISerializedItem GetItemByPath(string database, string path)
		{
			var physicalPath = GetSerializedItemPath(_rootPath, database, path);

			if (!File.Exists(physicalPath))
			{
				// check for a short-path version
				physicalPath = GetShortSerializedItemPath(_rootPath, database, path);

				if (!File.Exists(physicalPath))
					return null;
			}

			var reference = new SitecoreSerializedReference(physicalPath, this);

			return GetItem(reference);
		}

		public virtual ISerializedReference[] GetChildReferences(ISerializedReference parent, bool recursive)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var longPath = GetReferenceDirectoryPath(parent);
			var shortPath = GetShortSerializedReferencePath(_rootPath, parent);

			Func<string, string[]> parseDirectory = path =>
			{
				if (!Directory.Exists(path)) return new string[0];

				var resultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				try
				{
					string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);

					foreach (var file in files)
						resultSet.Add(file);

					string[] directories = GetDirectories(path);

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
				catch (DirectoryNotFoundException)
				{
					// it seems like occasionally, even though we use Directory.Exists() to make sure the parent dir exists, that when we actually call Directory.GetFiles()
					// it throws an error that the directory does not exist during recursive deletes. If the directory does not exist, then we can safely assume no children are present.
					return new string[0];
				}
			};

			var results = Enumerable.Concat(parseDirectory(longPath), parseDirectory(shortPath));

			List<ISerializedReference> referenceResults = results.Select(x => (ISerializedReference)new SitecoreSerializedReference(x, this)).ToList();

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

			var path = GetReferenceItemPath(reference);

			if (File.Exists(path)) return ReadItemFromDisk(path);

			var shortPath = GetShortSerializedItemPath(_rootPath, reference);

			if (File.Exists(shortPath)) return ReadItemFromDisk(shortPath);

			return null;
		}

		public virtual ISerializedItem[] GetChildItems(ISerializedReference parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var path = GetReferenceDirectoryPath(parent);
			var shortPath = GetShortSerializedReferencePath(_rootPath, parent);

			var fileNames = new List<string>();

			bool longPathExists = Directory.Exists(path);
			bool shortPathExists = Directory.Exists(shortPath);

			if (!longPathExists && !shortPathExists) return new ISerializedItem[0];

			if (longPathExists) fileNames.AddRange(Directory.GetFiles(path, "*" + PathUtils.Extension));
			if (shortPathExists) fileNames.AddRange(Directory.GetFiles(shortPath, "*" + PathUtils.Extension));

			return fileNames.Select(ReadItemFromDisk).ToArray();
		}

		public virtual ISourceItem DeserializeItem(ISerializedItem serializedItem, bool ignoreMissingTemplateFields)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");

			return _formatter.Deserialize(serializedItem, ignoreMissingTemplateFields);
		}

		public virtual void RenameSerializedItem(ISourceItem renamedItem, string oldName)
		{
			if (renamedItem == null || oldName == null) return;

			var typed = renamedItem as SitecoreSourceItem;

			if (typed == null) throw new ArgumentException("Renamed item must be a SitecoreSourceItem", "renamedItem");

			// write the serialized item under its new name
			var updatedItem = SerializeItem(renamedItem);

			// find the children directory path of the previous item name, if it exists, and move them to the new child path
			var oldItemPath = renamedItem.ItemPath.Substring(0, renamedItem.ItemPath.Length - renamedItem.Name.Length) + oldName;

			var oldSerializedChildrenDirectoryPath = GetSerializedReferencePath(_rootPath, renamedItem.DatabaseName, oldItemPath);

			var oldSerializedChildrenReference = new SitecoreSerializedReference(oldSerializedChildrenDirectoryPath, this);

			var shortOldSerializedChildrenPath = GetShortSerializedReferencePath(_rootPath, oldSerializedChildrenReference);
			var shortOldSerializedChildrenReference = new SitecoreSerializedReference(shortOldSerializedChildrenPath, this);

			if (Directory.Exists(oldSerializedChildrenReference.ProviderId))
				MoveDescendants(oldSerializedChildrenReference, updatedItem, renamedItem, true);

			if (Directory.Exists(shortOldSerializedChildrenPath))
				MoveDescendants(shortOldSerializedChildrenReference, updatedItem, renamedItem, true);

			// delete the original serialized item from pre-rename (unless the names only differ by case, in which case we'd delete the item entirely because NTFS is case insensitive!)
			if (!renamedItem.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase))
			{
				// note that we don't have to worry about short paths here because DeleteSerializedItem() knows how to find them
				DeleteSerializedItem(oldSerializedChildrenReference);
			}
		}

		public virtual void MoveSerializedItem(ISourceItem sourceItem, ISourceItem newParentItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(newParentItem, "newParentItem");

			var sitecoreSource = sourceItem as SitecoreSourceItem;
			var sitecoreParent = newParentItem as SitecoreSourceItem;

			if (sitecoreParent == null) throw new ArgumentException("newParentItem must be a SitecoreSourceItem", "newParentItem");
			if (sitecoreSource == null) throw new ArgumentException("sourceItem must be a SitecoreSourceItem", "sourceItem");

			var oldRootDirectory = new SitecoreSerializedReference(GetSerializedReferencePath(_rootPath, sourceItem), this);
			var oldRootItemPath = new SitecoreSerializedReference(GetReferenceItemPath(oldRootDirectory), this);

			var newRootItemPath = newParentItem.ItemPath + "/" + sourceItem.Name;
			var newRootSerializedPath = GetSerializedItemPath(_rootPath, newParentItem.DatabaseName, newRootItemPath);

			var syncItem = ItemSynchronization.BuildSyncItem(sitecoreSource.InnerItem);

			// update the path and parent IDs to the new location
			syncItem.ParentID = newParentItem.Id.ToString();
			syncItem.ItemPath = newRootItemPath;

			// if this occurs we're "moving" an item to the same location it started from. Which means we shouldn't do anything.
			if (oldRootDirectory.ItemPath.Equals(syncItem.ItemPath)) return;

			var serializedNewItem = new SitecoreSerializedItem(syncItem, newRootSerializedPath, this);

			// write the moved sync item to its new destination
			UpdateSerializedItem(serializedNewItem);

			// move any children to the new destination (and fix their paths)
			MoveDescendants(oldRootDirectory, serializedNewItem, sourceItem, false);

			// remove the serialized item in the old location
			DeleteSerializedItem(oldRootItemPath);
		}

		public virtual void DeleteSerializedItem(ISerializedReference item)
		{
			DeleteItemRecursive(item);

			CleanupObsoleteShortens();
		}

		protected virtual void DeleteItemRecursive(ISerializedReference reference)
		{
			foreach (var child in reference.GetChildReferences(false))
			{
				DeleteItemRecursive(child);
			}

			// kill the serialized file
			var fileItem = reference.GetItem();
			if (fileItem != null && File.Exists(fileItem.ProviderId)) File.Delete(fileItem.ProviderId);

			// remove any serialized children
			var directory = GetReferenceDirectoryPath(reference);

			if (Directory.Exists(directory)) Directory.Delete(directory, true);

			// clean up any hashpaths for this item
			var shortDirectory = GetShortSerializedReferencePath(_rootPath, reference);

			if (Directory.Exists(shortDirectory)) Directory.Delete(shortDirectory, true);

			// clean up empty parent folder(s)
			var parentDirectory = Directory.GetParent(directory);

			if (!parentDirectory.Exists) return;

			do
			{
				if (parentDirectory.GetFileSystemInfos().Length > 0) break;

				parentDirectory.Delete(true);
				parentDirectory = parentDirectory.Parent;

			} while (parentDirectory != null && parentDirectory.Exists);
		}

		protected virtual ISerializedItem ReadItemFromDisk(string fullPath)
		{
			try
			{
				using (var reader = File.OpenRead(fullPath))
				{
					return new SitecoreSerializedItem(_formatter.Read(reader), fullPath, this);
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error loading " + fullPath, ex);
			}
		}

		/// <summary>
		/// Moves the descendant items of a serialized parent after it has been moved or renamed.
		/// </summary>
		/// <param name="oldReference">Reference to the original path pre-move/rename</param>
		/// <param name="newItem">The newly renamed or moved parent item</param>
		/// <param name="sourceItem">The source item representing the renamed/moved item. NOTE that the path of this item is incorrect a lot of the time so we ignore it.</param>
		/// <param name="renaming">True for moving renamed children, false for moving moved children. For renames, the children already have correct new paths; for moves we have to recalculate it.</param>
		/// <remarks>
		/// This method basically gets all descendants of the source item that was moved/renamed, generates an appropriate new serialized item for it, and _if the new child item is in the predicate_ we
		/// serialize it to its new location. Finally, we delete the old children directory if it existed.
		/// 
		/// Doing it this way allows handling crazy cases like moving trees of items between included and excluded locations - or even moves or renames causing SOME of the children to be ignored. Wild.
		/// </remarks>
		protected virtual void MoveDescendants(ISerializedReference oldReference, ISerializedItem newItem, ISourceItem sourceItem, bool renaming)
		{
			// remove the extension from the new item's provider ID
			string newItemReferencePath = GetReferenceDirectoryPath(newItem);

			// if the paths were the same, no moving occurs (this can happen when saving templates, which spuriously can report "renamed" when they are not actually any such thing)
			if (oldReference.ProviderId.Equals(newItemReferencePath, StringComparison.Ordinal)) return;

			// this is for renaming an item that differs only by case from the original. Because NTFS is case-insensitive the 'new parent' exists
			// already, but it will use the old name. Not quite what we want. So we need to manually rename the folder.
			if (oldReference.ProviderId.Equals(newItemReferencePath, StringComparison.OrdinalIgnoreCase) && Directory.Exists(oldReference.ProviderId))
			{
				Directory.Move(oldReference.ProviderId, oldReference.ProviderId + "_tempunicorn");
				Directory.Move(oldReference.ProviderId + "_tempunicorn", newItemReferencePath);
			}

			var descendantItems = GetDescendants(sourceItem).Cast<SitecoreSourceItem>();

			// Go through descendant source items and serialize all that are included by the predicate
			foreach (var descendant in descendantItems)
			{
				var syncItem = ItemSynchronization.BuildSyncItem(descendant.InnerItem);

				// the newPhysicalPath will point to the OLD physical path pre-move (but for renames we actually get the new path already).
				// For moves, we re-root the path to point to the new parent item's base path to fix that before we write to disk
				string newItemPath = (renaming) ? descendant.ItemPath : descendant.ItemPath.Replace(oldReference.ItemPath, newItem.ItemPath);

				var newPhysicalPath = GetSerializedItemPath(_rootPath, syncItem.DatabaseName, newItemPath);

				var newSerializedItem = new SitecoreSerializedItem(syncItem, newPhysicalPath, this);

				if (!_predicate.Includes(newSerializedItem).IsIncluded) continue; // if the moved child location is outside the predicate, do not re-serialize

				UpdateSerializedItem(newSerializedItem);
			}

			// remove the old children folder if it exists - as long as the original name was not a case insensitive version of this item
			if (Directory.Exists(oldReference.ProviderId) && !oldReference.ProviderId.Equals(newItemReferencePath, StringComparison.OrdinalIgnoreCase))
			{
				Directory.Delete(oldReference.ProviderId, true);
			}
		}

		protected virtual IList<ISourceItem> GetDescendants(ISourceItem sourceItem)
		{
			var descendants = sourceItem.Children.ToList();

			foreach (var child in descendants.ToArray())
			{
				descendants.AddRange(GetDescendants(child));
			}

			return descendants;
		}

		protected virtual void UpdateSerializedItem(ISerializedItem serializedItem)
		{
			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			// create any requisite parent folder(s) for the serialized item
			var parentPath = Directory.GetParent(GetReferenceDirectoryPath(serializedItem));
			if (parentPath != null && !parentPath.Exists)
				Directory.CreateDirectory(parentPath.FullName);

			// if the file already exists, delete it. Why? Because the FS is case-insensitive, if an item is renamed by case only it won't actually rename
			// on the filesystem. Deleting it first makes sure this occurs.
			if (File.Exists(serializedItem.ProviderId)) File.Delete(serializedItem.ProviderId);

			using (var fileStream = File.Open(serializedItem.ProviderId, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				_formatter.Serialize(typed.InnerItem, fileStream);
			}
		}

		/// <summary>
		/// Performs cleanup for shorten folders which referenced link items were already deleted.
		/// </summary>
		protected virtual void CleanupObsoleteShortens()
		{
			foreach (string directory in Directory.GetDirectories(_rootPath))
			{
				string path = FileUtil.MakePath(directory, "link");
				if (File.Exists(path))
				{
					string linkContents = File.ReadAllText(path);

					ItemReference itemReference = ItemReference.Parse(linkContents.Replace('\\', '/'));
					if (itemReference != null && itemReference.GetItem() == null)
					{
						FileUtil.DeleteDirectory(directory, true);
					}
					else
					{
						// clean directories with only the link file, even if the link is valid (no items exist under the link)
						// (if the children length is 1 and we got here we know the link file exists and thus it MUST be the 1 item)
						if (Directory.GetFileSystemEntries(directory).Length == 1)
							Directory.Delete(directory, true);
					}
				}
				else
				{
					// clean empty directories
					if (Directory.GetFileSystemEntries(directory).Length == 0)
						Directory.Delete(directory);
				}
			}
		}

		/// <summary>
		/// The root path on disk this provider writes its files to
		/// </summary>
		public virtual string SerializationRoot { get { return _rootPath; } }

		/// <summary>
		/// Gets the physical path to the .item file that defines the source item. Returns the path regardless of if the item file exists.
		/// </summary>
		protected virtual string GetSerializedItemPath(string rootDirectory, ISourceItem sourceItem)
		{
			return GetSerializedReferencePath(rootDirectory, sourceItem) + PathUtils.Extension;
		}

		/// <summary>
		/// Gets the physical path to the .item file that defines the source item. Returns the path regardless of if the item file exists.
		/// WARNING: This overload may not work correctly with over-length paths due to a bug in Sitecore. Use the ISourceItem version whenever possible.
		/// </summary>
		protected virtual string GetSerializedItemPath(string rootDirectory, string database, string path)
		{
			return GetSerializedReferencePath(rootDirectory, database, path) + PathUtils.Extension;
		}

		/// <summary>
		/// Gets the short path to a serialized item. Note that this means the short path of the PARENT, combined with the item name of what's passed in
		/// </summary>
		protected virtual string GetShortSerializedItemPath(string rootDirectory, ISerializedReference reference)
		{
			return GetShortSerializedItemPath(rootDirectory, reference.DatabaseName, reference.ItemPath);
		}

		/// <summary>
		/// Gets the short path to a serialized item. Note that this means the short path of the PARENT, combined with the item name of what's passed in
		/// </summary>
		protected virtual string GetShortSerializedItemPath(string rootDirectory, string database, string itemPath)
		{
			var itemRootPath = itemPath.TrimEnd('/');

			int lastDirectoryIndex = itemRootPath.LastIndexOf('/');
			var parentItemPath = itemRootPath.Substring(0, lastDirectoryIndex);
			var contextItemName = itemRootPath.Substring(lastDirectoryIndex + 1);

			return string.Format("{0}\\{1}{2}", GetShortSerializedReferencePath(rootDirectory, database, parentItemPath), contextItemName, PathUtils.Extension);
		}

		/// <summary>
		/// Gets the physical path to the directory that contains children of the item path/database name
		/// </summary>
		protected virtual string GetSerializedReferencePath(string rootDirectory, ISourceItem sourceItem)
		{
			var sitecoreSourceItem = sourceItem as SitecoreSourceItem;

			Assert.IsNotNull(sitecoreSourceItem, "Source item must be a SitecoreSourceItem.");

			// ReSharper disable PossibleNullReferenceException
			return PathUtils.GetDirectoryPath(new ItemReference(sitecoreSourceItem.InnerItem).ToString(), rootDirectory);
			// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>
		/// Gets the physical path to the directory that contains children of the item path/database name. Returns the path regardless of if the directory exists.
		/// WARNING: This overload may not work correctly with over-length paths due to a bug in Sitecore. Use the ISourceItem version whenever possible.
		/// </summary>
		protected virtual string GetSerializedReferencePath(string rootDirectory, string database, string path)
		{
			return PathUtils.GetDirectoryPath(new ItemReference(database, path).ToString(), rootDirectory);
		}

		/// <summary>
		/// Gets the shortened version of a reference path. The short path is utilized when the actual path becomes longer than the OS allows paths to be, and is based on a hash.
		/// This method will return the short path regardless of if it exists, and will return it even for path lengths that do not require using the shortened version.
		/// </summary>
		protected virtual string GetShortSerializedReferencePath(string rootDirectory, ISerializedReference reference)
		{
			return GetShortSerializedReferencePath(rootDirectory, reference.DatabaseName, reference.ItemPath);
		}

		/// <summary>
		/// Gets the shortened version of a reference path. The short path is utilized when the actual path becomes longer than the OS allows paths to be, and is based on a hash.
		/// This method will return the short path regardless of if it exists, and will return it even for path lengths that do not require using the shortened version.
		/// </summary>
		protected virtual string GetShortSerializedReferencePath(string rootDirectory, string databaseName, string itemPath)
		{
			// note that wer're constructing a virtual path here and not using the ProviderId. This is because the provider ID could be a short path
			// and if it is, we would otherwise generate an invalid short path for this item (hashing a short path instead of the full path).
			var path = string.Format("{0}\\{1}", rootDirectory.TrimEnd('\\'), new ItemReference(databaseName, itemPath).ToString().Replace('/', '\\'));

			if (!path.StartsWith(rootDirectory, StringComparison.InvariantCultureIgnoreCase))
				throw new ArgumentException("Reference path is not under the serialization root!");

			return Path.Combine(rootDirectory, GetShortPathHash(path.Substring(rootDirectory.Length)));
		}

		/// <summary>
		/// A serialized reference might refer to a directory OR a serialized item file directly. This method makes sure we've got the directory version, if it refers to a file.
		/// </summary>
		protected virtual string GetReferenceDirectoryPath(ISerializedReference reference)
		{
			return PathUtils.StripPath(reference.ProviderId);
		}

		/// <summary>
		/// A serialized reference might refer to a directory OR a serialized item file directly. This method makes sure we've got the item version, if it refers to a directory.
		/// </summary>
		protected virtual string GetReferenceItemPath(ISerializedReference reference)
		{
			return reference.ProviderId.EndsWith(PathUtils.Extension, StringComparison.OrdinalIgnoreCase) ? reference.ProviderId : reference.ProviderId + PathUtils.Extension;
		}

		protected virtual string[] GetDirectories(string physicalPath)
		{
			if (!Directory.Exists(physicalPath))
				return new string[0];

			return Directory.GetDirectories(physicalPath);
		}

		protected virtual HashAlgorithm ShortPathHashAlgorithm
		{
			get { return MD5.Create(); }
		}

		/// <summary>
		/// This is an analog of the private PathUtils.GetHash() method. It is reproduced here because you cannot otherwise compute a short path for an arbitrary root path - only the default serialization path.
		/// </summary>
		protected virtual string GetShortPathHash(string path)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(path.ToLowerInvariant());

			ShortPathHashAlgorithm.Initialize();

			byte[] hash = ShortPathHashAlgorithm.ComputeHash(bytes);
			for (int index = 0; index < hash.Length; ++index)
			{
				hash[index % 4] ^= hash[index];
			}

			var stringBuilder = new StringBuilder();

			for (int index = 0; index < 4; ++index)
			{
				stringBuilder.Append(hash[index].ToString("X" + 2));
			}

			return stringBuilder.ToString();
		}

		public virtual string FriendlyName
		{
			get { return "Sitecore Serialization Provider"; }
		}

		public virtual string Description
		{
			get { return "Stores serialized items using Sitecore's built-in serialization engine."; }
		}

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new[] { new KeyValuePair<string, string>("Root path", _rootPath) };
		}
	}
}
