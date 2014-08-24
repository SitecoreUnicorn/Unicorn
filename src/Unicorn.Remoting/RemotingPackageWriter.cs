using System.IO;
using Sitecore.Data.Engines;
using Sitecore.Diagnostics;

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
			if (Directory.Exists(path))
				Directory.Delete(path, true);

			var sourcePath = new DirectoryInfo(Path.Combine(_package.TempDirectory, "serialization"));
			var targetPath = new DirectoryInfo(path);

			targetPath.Create();
			CopyFilesRecursively(sourcePath, targetPath);
		}

		private void WriteDiffPackage(string path)
		{
			var actions = _package.Manifest.HistoryEntries;

			foreach (var action in actions)
			{
				switch (action.Action)
				{
					case HistoryAction.AddedVersion:
					case HistoryAction.Created:
					case HistoryAction.Saved:
						// TODO: copy single item and overwrite existing
						
						break;

					case HistoryAction.Deleted:
						// TODO: delete item
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
