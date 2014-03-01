using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Sitecore;
using Sitecore.Data.Serialization;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Unicorn.Data;

namespace Unicorn.Serialization.Sitecore
{
	/// <summary>
	/// This class reimplements or normalizes the implementation of a lot of Sitecore's PathUtils class.
	/// That class unfortunately hates strong typing, and many of its methods simply do not support an explicitly specified
	/// serialization root path at all. These implementations get around that.
	/// </summary>
	public static class SerializationPathUtility
	{
		/// <summary>
		/// Gets the physical path to the .item file that defines the source item. Returns the path regardless of if the item file exists.
		/// </summary>
		public static string GetSerializedItemPath(string rootDirectory, ISourceItem sourceItem)
		{
			return GetSerializedReferencePath(rootDirectory, sourceItem) + PathUtils.Extension;
		}

		/// <summary>
		/// Gets the physical path to the directory that contains children of the item path/database name
		/// </summary>
		public static string GetSerializedReferencePath(string rootDirectory, ISourceItem sourceItem)
		{
			var sitecoreSourceItem = sourceItem as SitecoreSourceItem;
			
			Assert.IsNotNull(sitecoreSourceItem, "Source item must be a SitecoreSourceItem.");

// ReSharper disable PossibleNullReferenceException
			return PathUtils.GetDirectoryPath(new ItemReference(sitecoreSourceItem.InnerItem).ToString(), rootDirectory);
// ReSharper restore PossibleNullReferenceException
		}

		/// <summary>
		/// Gets the shortened version of a reference path. The short path is utilized when the actual path becomes longer than the OS allows paths to be, and is based on a hash.
		/// This method will return the short path regardless of if it exists, and will return it even for path lengths that do not require using the shortened version.
		/// </summary>
		public static string GetShortSerializedReferencePath(string rootDirectory, ISerializedReference reference)
		{
			var path = reference.ProviderId;

			if (!path.StartsWith(rootDirectory, StringComparison.InvariantCultureIgnoreCase))
				throw new ArgumentException("Reference path is not under the serialization root!");

			return Path.Combine(rootDirectory, GetShortPathHash(path.Substring(rootDirectory.Length)));
		}

		/// <summary>
		/// A serialized reference might refer to a directory OR a serialized item file directly. This method makes sure we've got the directory version, if it refers to a file.
		/// </summary>
		public static string GetReferenceDirectoryPath(ISerializedReference reference)
		{
			return PathUtils.StripPath(reference.ProviderId);
		}

		/// <summary>
		/// A serialized reference might refer to a directory OR a serialized item file directly. This method makes sure we've got the item version, if it refers to a directory.
		/// </summary>
		public static string GetReferenceItemPath(ISerializedReference reference)
		{
			return reference.ProviderId.EndsWith(PathUtils.Extension, StringComparison.OrdinalIgnoreCase) ? reference.ProviderId : reference.ProviderId + PathUtils.Extension;
		}

		public static string[] GetDirectories(string physicalPath, string serializationRootPath)
		{
			if (!Directory.Exists(physicalPath))
				return new string[0];

			var allDirectories = new List<string>(Directory.GetDirectories(physicalPath));

			string relativePath = StringUtil.Mid(physicalPath, serializationRootPath.Length);
			string shortPathRelativePath;
			if (string.IsNullOrEmpty(relativePath))
				shortPathRelativePath = string.Empty;
			else
				shortPathRelativePath = relativePath.Split(new []{Path.DirectorySeparatorChar})[0];

			string[] shortLinkFiles = Directory.GetFiles(serializationRootPath + shortPathRelativePath, "link");
			bool hasShortLinkFile = shortLinkFiles.Length == 1;

			string shortLinkFileContent = string.Empty;
			if (hasShortLinkFile)
			{
				shortLinkFileContent = File.ReadAllText(shortLinkFiles.First());
			}

			// this loop finds any short-links (serialization filenames too long for OS) and virtually adds them to the child list
			foreach (string file in Directory.GetFiles(physicalPath))
			{
				string shortPath = string.Empty;
				if (hasShortLinkFile)
				{
					try
					{
						string shortLinkTargetHashName = FileUtil.MakePath(StringUtil.Mid(relativePath, GetShortPathHash(relativePath).Length), Path.GetFileNameWithoutExtension(file));
						string shortLinkTargetPath = FileUtil.MakePath(shortLinkFileContent, shortLinkTargetHashName);
						if (!string.IsNullOrEmpty(shortLinkTargetPath))
						{
							var reference = new SitecoreSerializedReference(serializationRootPath + shortLinkTargetPath.Replace('/', '\\'), null);
							shortPath = GetShortSerializedReferencePath(serializationRootPath, reference);
						}
					}
// ReSharper disable once EmptyGeneralCatchClause
					catch
					{
					}
				}

				if (string.IsNullOrEmpty(shortPath))
				{
					var reference = new SitecoreSerializedReference(PathUtils.StripPath(file), null);
					shortPath = GetShortSerializedReferencePath(serializationRootPath, reference);
				}

				if (Directory.Exists(shortPath))
					allDirectories.Add(shortPath);
			}

			return allDirectories.ToArray();
		}

		private static readonly MD5 ShortPathHashAlgorithm = MD5.Create();

		/// <summary>
		/// This is an analog of the private PathUtils.GetHash() method. It is reproduced here because you cannot otherwise compute a short path for an arbitrary root path - only the default serialization path.
		/// </summary>
		private static string GetShortPathHash(string path)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(path.ToLowerInvariant());

			ShortPathHashAlgorithm.Initialize();

			byte[] hash = ShortPathHashAlgorithm.ComputeHash(bytes);
			for (int index = 0; index < hash.Length; ++index)
			{
				hash[index%4] ^= hash[index];
			}

			var stringBuilder = new StringBuilder();

			for (int index = 0; index < 4; ++index)
			{
				stringBuilder.Append(hash[index].ToString("X" + 2));
			}

			return stringBuilder.ToString();
		}
	}
}
