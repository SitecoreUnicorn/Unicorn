using System.IO;
using Sitecore.Data.Engines;
using Sitecore.Diagnostics;
using Unicorn.Serialization.Sitecore;

namespace Unicorn.Remoting
{
	public class RemotingPackageWriter
	{
		private readonly RemotingPackage _package;

		public RemotingPackageWriter(RemotingPackage package)
		{
			_package = package;
		}

		public void WriteTo(string path)
		{
			Assert.IsTrue(_package.Manifest.Strategy != RemotingStrategy.Differential || Directory.Exists(path), "Invalid target directory! Must exist for differential strategy.");

			if (_package.Manifest.Strategy == RemotingStrategy.Full)
			{
				WriteFullPackage(path);
			}
			else
			{
				WriteDiffPackage(path);
			}
		}

		private void WriteFullPackage(string path)
		{
			// TODO: this is super destructive and will nuke your whole serialization folder
			// TODO: instead of just the root that you got in the package. 1-800-L2C-NOOB
			if (Directory.Exists(path))
				Directory.Delete(path, true);

			var sourcePath = new DirectoryInfo(Path.Combine(_package.TempDirectory, "serialization"));
			var targetPath = new DirectoryInfo(path);

			targetPath.Create();
			CopyFilesRecursively(sourcePath, targetPath);
		}

		private void WriteDiffPackage(string targetBasePath)
		{
			var actions = _package.Manifest.HistoryEntries;
			var packageBasePath = Path.Combine(_package.TempDirectory, "serialization");

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

						File.Copy(itemPath, targetCopyPath, true);

						break;

					case HistoryAction.Deleted:
						// delete item + child references
						var targetDeletePath = SerializationPathUtility.GetSerializedItemPath(targetBasePath, action.Database, action.ItemPath);
						var targetChildrenDeletePath = SerializationPathUtility.GetSerializedReferencePath(targetBasePath, action.Database, action.ItemPath);

						if(File.Exists(targetDeletePath)) File.Delete(targetDeletePath);
						if(Directory.Exists(targetChildrenDeletePath)) Directory.Delete(targetChildrenDeletePath, true);

						break;

					case HistoryAction.Moved:
						// TODO: move item? or delete old path and copy new?
						break;
				}
			}
		}

		private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
		{
			foreach (DirectoryInfo dir in source.GetDirectories())
				CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

			foreach (FileInfo file in source.GetFiles())
				file.CopyTo(Path.Combine(target.FullName, file.Name));
		}
	}
}
