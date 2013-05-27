using System;
using System.IO;
using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;

namespace Unicorn.Serialization
{
	public class SitecoreSerializationProvider : ISerializationProvider
	{
		public void SerializeSingleItem(Item item)
		{
			throw new NotImplementedException();
		}

		public ISerializedReference GetReference(string sitecorePath, string databaseName)
		{
			var reference = new ItemReference(databaseName, sitecorePath);

			var physicalPath = PathUtils.GetDirectoryPath(reference.ToString());

			if (!Directory.Exists(physicalPath)) throw new FileNotFoundException("The root serialization path " + physicalPath + " did not exist!", physicalPath);

			throw new Exception();
		}

		public ISerializedReference[] GetChildReferences(ISerializedReference parent)
		{
			throw new NotImplementedException();
			/*
			 
			private void LoadTreePaths(string physicalPath, LoaderParameters options)
			{
				DoLoadTree(physicalPath, options);
				DoLoadTree(PathUtils.GetShortPath(physicalPath), options);
				options.Progress.ReportStatus(string.Format("Finished loading serialized items from {0} ({1} total items synchronized)", physicalPath, _itemsProcessed), MessageType.Info);
			}
			 
			// check if we have a directory path to recurse down
				if (Directory.Exists(path))
				{
					string[] directories = PathUtils.GetDirectories(path);

					// make sure if a "templates" item exists in the current set, it goes first
					if (directories.Length > 1)
					{
						for (int i = 1; i < directories.Length; i++)
						{
							if ("templates".Equals(Path.GetFileName(directories[i]), StringComparison.OrdinalIgnoreCase))
							{
								string text = directories[0];
								directories[0] = directories[i];
								directories[i] = text;
							}
						}
					}

					foreach(var directory in directories)
					{
						if (!CommonUtils.IsDirectoryHidden(directory))
						{
							LoadTreeRecursive(directory, options, retryList);
						}
					}
			 */
		}


		public ISerializedItem GetItem(ISerializedReference reference)
		{
			// note PathUtils.StripPath(fileName) will turn an item path into a dir
			throw new NotImplementedException();
		}

		public ISerializedItem[] GetChildItems(ISerializedReference parent)
		{
			/*
			 if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);
				foreach (string fileName in files)
			 */
			throw new Exception();
		}


		public Item DeserializeItem(ISerializedItem serializedItem, IProgressStatus progress)
		{
			//var options = new LoadOptions { DisableEvents = true, ForceUpdate = true, UseNewID = false };

			//result = ItemSynchronization.PasteSyncItem(serializedItem, newOptions, true);

			/*
			 catch (ParentItemNotFoundException ex)
				{
					result = null;
					loadResult = ItemLoadStatus.Error;
					string error =
						"Cannot load item from path '{0}'. Probable reason: parent item with ID '{1}' not found.".FormatWith(
							PathUtils.UnmapItemPath(path, options.Root), ex.ParentID);

					options.Progress.ReportStatus(error, MessageType.Error);

					LogLocalizedError(error);
				}
				catch (ParentForMovedItemNotFoundException ex2)
				{
					result = ex2.Item;
					loadResult = ItemLoadStatus.Error;
					string error =
						"Item from path '{0}' cannot be moved to appropriate location. Possible reason: parent item with ID '{1}' not found."
							.FormatWith(PathUtils.UnmapItemPath(path, options.Root), ex2.ParentID);

					options.Progress.ReportStatus(error, MessageType.Error);

					LogLocalizedError(error);
				}
				return result;*/

			throw new Exception();
		}
	}
}
