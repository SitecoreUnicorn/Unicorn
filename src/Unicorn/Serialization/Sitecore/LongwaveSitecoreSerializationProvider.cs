using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization.Sitecore.Formatting;
using Alphaleonis.Win32.Filesystem;
using Sitecore.Data.Serialization;

namespace Unicorn.Serialization.Sitecore
{
	public class LongwaveSitecoreSerializationProvider : SitecoreSerializationProvider
	{
		private readonly ISitecoreSerializationFormatter _formatter;

		public LongwaveSitecoreSerializationProvider(IPredicate predicate, ISitecoreSerializationFormatter formatter, string rootPath = null, string logName = "UnicornItemSerialization")
			: base(predicate, formatter, rootPath, logName)
		{
			_formatter = formatter;
		}

		public override ISerializedReference[] GetChildReferences(ISerializedReference parent, bool recursive)
		{
			Assert.ArgumentNotNull(parent, "parent");

			var path = GetReferenceDirectoryPath(parent);

			if (path == null || !DirectoryExists(path)) return new ISerializedReference[0];

			var resultSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			string[] files = GetChildFiles(path, "*" + PathUtils.Extension);

			foreach (var file in files)
				resultSet.Add(file);

			string[] directories = GetDirectories(path);

			// add directories that aren't already ref'd indirectly by a file
			foreach (var directory in directories)
			{
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

			var referenceResults = resultArray.Select(x => (ISerializedReference)new SitecoreSerializedReference(x, this)).ToList();

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

		protected override void DeleteItemRecursive(ISerializedReference reference)
		{
			foreach (var child in reference.GetChildReferences(false))
			{
				DeleteItemRecursive(child);
			}

			// kill the serialized file
			var fileItem = reference.GetItem();
			if (fileItem != null && FileExists(fileItem.ProviderId)) DeleteFile(fileItem.ProviderId);

			// remove any serialized children
			var directory = GetReferenceDirectoryPath(reference);

			if (DirectoryExists(directory)) DeleteDirectory(directory, true);

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

		protected override void UpdateSerializedItem(ISerializedItem serializedItem)
		{
			var typed = serializedItem as SitecoreSerializedItem;

			if (typed == null) throw new ArgumentException("Serialized item must be a SitecoreSerializedItem", "serializedItem");

			// create any requisite parent folder(s) for the serialized item
			var parentPath = Directory.GetParent(GetReferenceDirectoryPath(serializedItem));
			if (parentPath != null && !parentPath.Exists)
				Directory.CreateDirectory(parentPath.FullName);

			// if the file already exists, delete it. Why? Because the FS is case-insensitive, if an item is renamed by case only it won't actually rename
			// on the filesystem. Deleting it first makes sure this occurs.
			if (FileExists(serializedItem.ProviderId)) DeleteFile(serializedItem.ProviderId);

			using (var fileStream = File.Open(serializedItem.ProviderId, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
			{
				_formatter.Serialize(typed.InnerItem, fileStream);
			}
		}

		protected override void CleanupObsoleteShortens()
		{
			// do nothing
		}

		protected override ISerializedItem ReadItemFromDisk(string fullPath)
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

		protected override string GetShortSerializedItemPath(string rootDirectory, string database, string itemPath)
		{
			return null;
		}

		protected override string GetShortSerializedReferencePath(string rootDirectory, string databaseName, string itemPath)
		{
			return null;
		}

		protected override string GetSerializedReferencePath(string rootDirectory, ISourceItem sourceItem)
		{
			return GetSerializedReferencePath(rootDirectory, sourceItem.DatabaseName, sourceItem.ItemPath);
		}

		protected override string GetSerializedReferencePath(string rootDirectory, string database, string path)
		{
			var itemReference = new ItemReference(database, path).ToString();
			return MapItemPath(itemReference, SerializationRoot);
		}
		
		#region PathUtils
		protected virtual string MapItemPath(string itemPath, string root)
		{
			Assert.IsFalse(Path.IsPathRooted(itemPath), "itemPath is rooted");
			itemPath = ReplaceIllegalCharsByConfig(itemPath);

			return Path.Combine(root, itemPath).Replace('/', Path.DirectorySeparatorChar);
		}

		private static string ReplaceIllegalCharsByConfig(string path)
		{
			Assert.ArgumentNotNull(path, "path");
			return HandleIllegalSymbols(path, pair => pair.Key.ToString(), pair => pair.Value);
		}

		private static readonly char[] IllegalCharacters = new char[2] {'%','$'};
		private static readonly Func<char, string> EncodingAlgorithm = inp => '%'.ToString() + ((int)inp).ToString("X2");
		private static string HandleIllegalSymbols(string str, Func<KeyValuePair<char, string>, string> keySelector, Func<KeyValuePair<char, string>, string> valueSelector)
		{
			return IllegalSymbolsToReplace.Aggregate(new StringBuilder(str), (current, pair) => current.Replace(keySelector(pair), valueSelector(pair))).ToString();
		}

		// it's not mine, I swear!
		private static readonly List<KeyValuePair<char, string>> IllegalSymbolsToReplace = IllegalCharacters
			.Concat(Path.GetInvalidFileNameChars())
			.Concat(global::Sitecore.Configuration.Settings.Serialization.InvalidFileNameChars)
			.Distinct()
			.Except(new[]
				{
					Path.AltDirectorySeparatorChar,
					Path.DirectorySeparatorChar
				})
			.Select(ch => new KeyValuePair<char, string>(ch, EncodingAlgorithm(ch)))
			.ToList();
		#endregion

		#region I/O Wrapper
		protected override bool DirectoryExists(string path)
		{
			return Directory.Exists(path);
		}

		protected override bool FileExists(string path)
		{
			return File.Exists(path);
		}

		protected override void DeleteFile(string path)
		{
			File.Delete(path);
		}

		protected override string[] GetChildDirectories(string path)
		{
			return Directory.GetDirectories(path);
		}

		protected override string[] GetChildFiles(string path, string filter)
		{
			return Directory.GetFiles(path, filter);
		}

		protected override string CombinePaths(string path1, string path2)
		{
			return Path.Combine(path1, path2);
		}

		protected override void DeleteDirectory(string path, bool recursive)
		{
			Directory.Delete(path, recursive);
		}

		protected override void MoveDirectory(string path, string newPath)
		{
			Directory.Move(path, newPath);
		}
		#endregion

		public override string FriendlyName
		{
			get { return "Longwave Serialization Provider"; }
		}

		public override string Description
		{
			get { return "Standard Sitecore serialization structure but using NTFS long paths instead of short hash-paths. Note that you may need to interact with long paths with a tool other than Windows Explorer (e.g. 7-zip file manager), and tools that are not NTFS-aware may fail."; }
		}
	}
}
