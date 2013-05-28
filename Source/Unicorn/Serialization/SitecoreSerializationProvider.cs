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

			if (!Directory.Exists(physicalPath)) throw new FileNotFoundException("The reference path " + physicalPath + " did not exist!", physicalPath);

			return new SitecoreSerializedReference(physicalPath);
		}

		public ISerializedReference[] GetChildReferences(ISerializedReference parent)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var longPath = PathUtils.StripPath(parent.ProviderId);
			var shortPath = PathUtils.GetShortPath(longPath);

			Func<string, string[]> parseDirectory = path =>
				{
					if (Directory.Exists(longPath))
					{
						string[] directories = PathUtils.GetDirectories(longPath);

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

						return directories.Where(x => !CommonUtils.IsDirectoryHidden(x)).ToArray();
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
