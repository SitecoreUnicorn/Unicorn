using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kamsar.WebConsole;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Data.Serialization.ObjectModel;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Unicorn.Serialization
{
	public class SitecoreSerializationProvider : ISerializationProvider
	{
		public void SerializeItem(Item item)
		{
			Manager.DumpItem(item);
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

		public Item DeserializeItem(ISerializedItem serializedItem, IProgressStatus progress)
		{
			Assert.ArgumentNotNull(serializedItem, "serializedItem");
			Assert.ArgumentNotNull(progress, "progress");

			
			var typed = serializedItem as SitecoreSerializedItem;

			if(typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			try
			{
				var options = new LoadOptions { DisableEvents = true, ForceUpdate = true, UseNewID = false };

				return ItemSynchronization.PasteSyncItem(typed.InnerItem, options, true);
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
	}
}
