using System.IO;
using Sitecore.Data.Engines;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Unicorn.Predicates;
using Unicorn.Serialization;
using Unicorn.Serialization.Sitecore;

namespace Unicorn.Remoting
{
	public class RemotingPackageWriter
	{
		private readonly ISerializationProvider _serializationProvider;
		private readonly PredicateRootPathResolver _pathResolver;

		public RemotingPackageWriter(ISerializationProvider serializationProvider, PredicateRootPathResolver pathResolver)
		{
			_serializationProvider = serializationProvider;
			_pathResolver = pathResolver;
		}

		public void WriteTo(RemotingPackage package, string path)
		{
			Assert.IsTrue(package.Manifest.Strategy != RemotingStrategy.Differential || Directory.Exists(path), "Invalid target directory! Must exist for differential strategy.");

			if (package.Manifest.Strategy == RemotingStrategy.Full)
			{
				WriteFullPackage(package, path);
			}
			else
			{
				WriteDiffPackage(package, path);
			}
		}

		private void WriteFullPackage(RemotingPackage package, string path)
		{
			// get rid of any existing serialized items before we overwrite them
			var roots = _pathResolver.GetRootSourceItems();

			foreach (var root in roots)
			{
				var rootReference = _serializationProvider.GetReference(root);
				if (rootReference != null)
				{
					rootReference.Delete();
				}
			}

			var sourcePath = new DirectoryInfo(Path.Combine(package.TempDirectory, "serialization"));
			var targetPath = new DirectoryInfo(path);

			targetPath.Create();
			CopyFilesRecursively(sourcePath, targetPath);
		}

		private void WriteDiffPackage(RemotingPackage package, string targetBasePath)
		{
			var actions = package.Manifest.HistoryEntries;
			var packageBasePath = Path.Combine(package.TempDirectory, "serialization");

			foreach (var action in actions)
			{
				var itemPath = SerializationPathUtility.GetSerializedItemPath(packageBasePath, action.Database, action.ItemPath);
				switch (action.Action)
				{
					case HistoryAction.AddedVersion:
					case HistoryAction.Created:
					case HistoryAction.Saved:
						// copy single item and overwrite existing if any
						var targetCopyPath = SerializationPathUtility.GetSerializedItemPath(targetBasePath, action.Database, action.ItemPath);

						Assert.IsTrue(File.Exists(itemPath), "Expected serialization item {0} missing from package!", itemPath);

						Directory.CreateDirectory(Path.GetDirectoryName(targetCopyPath));
						File.Copy(itemPath, targetCopyPath, true);

						break;

					case HistoryAction.Deleted:
						// delete item + child references
						var targetDeletePath = SerializationPathUtility.GetSerializedItemPath(targetBasePath, action.Database, action.ItemPath);
						var targetChildrenDeletePath = SerializationPathUtility.GetSerializedReferencePath(targetBasePath, action.Database, action.ItemPath);

						if (File.Exists(targetDeletePath)) File.Delete(targetDeletePath);
						if (Directory.Exists(targetChildrenDeletePath)) Directory.Delete(targetChildrenDeletePath, true);

						break;

					case HistoryAction.Moved:
						var targetOldPath = SerializationPathUtility.GetSerializedItemPath(targetBasePath, action.Database, action.OldItemPath);
						var targetOldChildrenPath = SerializationPathUtility.GetSerializedReferencePath(targetBasePath, action.Database, action.ItemPath);

						if (File.Exists(targetOldPath)) File.Delete(targetOldPath);
						if (Directory.Exists(targetOldChildrenPath)) Directory.Delete(targetOldChildrenPath, true);

						var targetNewPath = SerializationPathUtility.GetSerializedItemPath(targetBasePath, action.Database, action.ItemPath);
						var targetNewChildrenPath = SerializationPathUtility.GetSerializedReferencePath(targetBasePath, action.Database, action.ItemPath);

						var itemChildrenPath = SerializationPathUtility.GetSerializedReferencePath(packageBasePath, action.Database, action.ItemPath);

						Directory.CreateDirectory(Path.GetDirectoryName(targetNewPath));
						File.Copy(itemPath, targetNewPath, true);

						if (Directory.Exists(itemChildrenPath))
						{
							Directory.CreateDirectory(targetNewChildrenPath);
							CopyFilesRecursively(new DirectoryInfo(itemChildrenPath), new DirectoryInfo(targetNewChildrenPath));
						}
				break;
				}
			}
		}

		private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
		{
			foreach (DirectoryInfo dir in source.GetDirectories())
				CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

			foreach (FileInfo file in source.GetFiles())
				file.CopyTo(Path.Combine(target.FullName, file.Name), true);
		}
	}
}
