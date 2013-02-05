using Kamsar.WebConsole;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Exceptions;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.StringExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.Data.Serialization.ObjectModel;

namespace Unicorn
{
	/// <summary>
	/// Custom loader that processes serialization loading with progress and additional rules options
	/// </summary>
	public class SerializationLoader
	{
		private int _itemsProcessed = 0;

		/// <summary>
		/// Loads a preset from serialized items on disk.
		/// </summary>
		public void LoadTree(AdvancedLoadOptions options)
		{
			Assert.ArgumentNotNull(options, "options");

			_itemsProcessed = 0;

			var reference = new ItemReference(options.Preset.Database, options.Preset.Path);
			var physicalPath = PathUtils.GetDirectoryPath(reference.ToString());

			options.Progress.ReportStatus("Loading serialized items from " + physicalPath, MessageType.Info);

			if (options.DisableEvents)
			{
				using (new EventDisabler())
				{
					LoadTreePaths(physicalPath, options);
				}

				string targetDatabase = GetTargetDatabase(physicalPath, options);
				DeserializationFinished(targetDatabase);

				return;
			}

			LoadTreePaths(physicalPath, options);	
		}

		private void LoadTreePaths(string physicalPath, AdvancedLoadOptions options)
		{
			DoLoadTree(physicalPath, options);
			DoLoadTree(PathUtils.GetShortPath(physicalPath), options);
			options.Progress.ReportStatus(string.Format("Finished loading serialized items from {0} ({1} total items synchronized)", physicalPath, _itemsProcessed), MessageType.Info);
		}

		/// <summary>
		/// Loads a specific path recursively, using any exclusions in the options' preset
		/// </summary>
		private void DoLoadTree(string path, AdvancedLoadOptions options)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNull(options, "options");
			var failures = new List<Failure>();

			// go load the tree and see what failed, if anything
			LoadTreeRecursive(path, options, failures);

			if (failures.Count > 0)
			{
				List<Failure> originalFailures;
				do
				{
					foreach (Database current in Factory.GetDatabases())
					{
						current.Engines.TemplateEngine.Reset();
					}

					// note tricky variable handling here, 'failures' used for two things
					originalFailures = failures;
					failures = new List<Failure>();

					foreach (var failure in originalFailures)
					{
						// retry loading a single item failure
						if (failure.Directory.EndsWith(PathUtils.Extension, StringComparison.InvariantCultureIgnoreCase))
						{
							try
							{
								ItemLoadResult result;
								DoLoadItem(failure.Directory, options, out result);
							}
							catch (Exception reason)
							{
								failures.Add(new Failure(failure.Directory, reason));
							}

							continue;
						}

						// retry loading a directory item failure (note the continues in the above ensure execution never arrives here for files)
						LoadTreeRecursive(failure.Directory, options, failures);
					}
				}
				while (failures.Count > 0 && failures.Count < originalFailures.Count); // continue retrying until all possible failures have been fixed
			}

			if (failures.Count > 0)
			{
				foreach (var failure in failures)
				{
					options.Progress.ReportStatus(string.Format("Failed to load {0} permanently because {1}", failure.Directory, failure.Reason), MessageType.Error);
				}

				throw new Exception("Some directories could not be loaded: " + failures[0].Directory, failures[0].Reason);
			}
		}

		/// <summary>
		/// Recursive method that loads a given tree and retries failures already present if any
		/// </summary>
		private void LoadTreeRecursive(string path, AdvancedLoadOptions options, List<Failure> retryList)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNull(options, "options");
			Assert.ArgumentNotNull(retryList, "retryList");

			if (options.Preset.Exclude.MatchesPath(path))
			{
				options.Progress.ReportStatus("[SKIPPED] " + PathUtils.MakeItemPath(path) + " (and children) because it was excluded by the preset, but it was present on disk", MessageType.Warning);
				return;
			}

			try
			{
				// load the current level
				LoadOneLevel(path, options, retryList);

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

					// pull out any standard values failures for immediate retrying
					List<Failure> standardValuesFailures = retryList.Where(x => x.Reason is StandardValuesException).ToList();
					retryList.RemoveAll(x => x.Reason is StandardValuesException);

					foreach (Failure current in standardValuesFailures)
					{
						try
						{
							ItemLoadResult result;
							DoLoadItem(current.Directory, options, out result);
						}
						catch (Exception reason)
						{
							try
							{
								var directoryInfo = new DirectoryInfo(current.Directory);
								if (directoryInfo.Parent != null && string.Compare(directoryInfo.Parent.FullName, path, StringComparison.InvariantCultureIgnoreCase) == 0)
								{
									retryList.Add(new Failure(current.Directory, reason));
								}
								else
								{
									retryList.Add(current);
								}
							}
							catch
							{
								retryList.Add(new Failure(current.Directory, reason));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				retryList.Add(new Failure(path, ex));
			}
		}

		/// <summary>
		/// Loads a set of children from a serialized path
		/// </summary>
		private void LoadOneLevel(string path, AdvancedLoadOptions options, List<Failure> retryList)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNull(options, "options");
			Assert.ArgumentNotNull(retryList, "retryList");

			var deleteCandidates = new Dictionary<ID, Item>();

			// look for a serialized file for the root item
			if (File.Exists(path + PathUtils.Extension))
			{
				var itemReference = ItemReference.Parse(PathUtils.MakeItemPath(path, options.Root));
				if (itemReference != null)
				{
					// get the corresponding item from Sitecore
					Item rootItem = options.Database != null
										? itemReference.GetItemInDatabase(options.Database)
										: itemReference.GetItem();

					// if we're reverting, we add all of the root item's direct children to the "to-delete" list (we'll remove them as we find matching serialized children)
					if (rootItem != null && (options.ForceUpdate || options.DeleteOrphans))
					{
						foreach (Item child in rootItem.Children)
						{
							// if the preset includes the child add it to the delete-candidate list (if we don't deserialize it below, it will be deleted if the right options are present)
							if(options.Preset.Includes(child))
								deleteCandidates[child.ID] = child;
							else
							{
								options.Progress.ReportStatus(string.Format("[SKIPPED] {0}:{1} (and children) because it was excluded by the preset.", child.Database.Name, child.Paths.FullPath), MessageType.Debug);
							}
						}
					}
				}
			}

			// check for a directory containing children of the target path
			if (Directory.Exists(path))
			{
				string[] files = Directory.GetFiles(path, "*" + PathUtils.Extension);
				foreach (string fileName in files)
				{
					try
					{
						ID itemId;
						if (IsStandardValuesItem(fileName, out itemId))
						{
							deleteCandidates.Remove(itemId); // avoid deleting standard values items when forcing an update
							retryList.Add(new Failure(fileName, new StandardValuesException(fileName)));
						}
						else
						{
							// load a child item
							ItemLoadResult result;
							Item loadedItem = DoLoadItem(fileName, options, out result);
							if (loadedItem != null)
							{
								deleteCandidates.Remove(loadedItem.ID);

								// check if we have any child directories under this loaded child item (existing children) -
								// if we do not, we can nuke any children of the loaded item as well
								if ((options.ForceUpdate || options.DeleteOrphans) && !Directory.Exists(PathUtils.StripPath(fileName)))
								{
									foreach (Item child in loadedItem.Children)
									{
										deleteCandidates.Add(child.ID, child);
									}
								}
							}
							else if (result == ItemLoadResult.Skipped) // if the item got skipped we'll prevent it from being deleted
								deleteCandidates.Remove(itemId);
						}
					}
					catch (Exception ex)
					{
						// if a problem occurs we attempt to retry later
						retryList.Add(new Failure(path, ex));
					}
				}
			}

			// if we're forcing an update (ie deleting stuff not on disk) we send the items that we found that weren't on disk off to get deleted from Sitecore
			if ((options.ForceUpdate || options.DeleteOrphans) && deleteCandidates.Count > 0)
			{
				Database db = deleteCandidates.Values.First().Database;

				bool reset = DeleteItems(deleteCandidates.Values, options.Progress);

				if (reset)
					db.Engines.TemplateEngine.Reset();
			}
		}

		/// <summary>
		/// Loads a specific item from disk
		/// </summary>
		private Item DoLoadItem(string path, AdvancedLoadOptions options, out ItemLoadResult loadResult)
		{
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNull(options, "options");

			if (File.Exists(path))
			{
				using (TextReader fileReader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
				{
					LogLocalized("Loading item from path {0}.", new object[]
						{
							PathUtils.UnmapItemPath(path, options.Root)
						});

					bool disabledLocally = ItemHandler.DisabledLocally;
					try
					{
						ItemHandler.DisabledLocally = true;
						Item result = null;
						try
						{
							var serializedItem = SyncItem.ReadItem(new Tokenizer(fileReader));

							_itemsProcessed++;
							if(_itemsProcessed % 500 == 0 && _itemsProcessed > 1)
								options.Progress.ReportStatus(string.Format("Processed {0} items", _itemsProcessed), MessageType.Debug);

							if (options.Preset.Exclude.MatchesTemplate(serializedItem.TemplateName))
							{
								options.Progress.ReportStatus(string.Format("[SKIPPED] {0}:{1} because the preset excluded its template name, but the item was on disk", serializedItem.DatabaseName, serializedItem.ItemPath), MessageType.Warning);

								loadResult = ItemLoadResult.Skipped;
								return null;
							}

							if (options.Preset.Exclude.MatchesTemplateId(serializedItem.TemplateID))
							{
								options.Progress.ReportStatus(string.Format("[SKIPPED] {0}:{1} because the preset excluded its template ID, but the item was on disk", serializedItem.DatabaseName, serializedItem.ItemPath), MessageType.Warning);

								loadResult = ItemLoadResult.Skipped;
								return null;
							}

							if (options.Preset.Exclude.MatchesId(serializedItem.ID))
							{
								options.Progress.ReportStatus(string.Format("[SKIPPED] {0}:{1} because the preset excluded it by ID, but the item was on disk", serializedItem.DatabaseName, serializedItem.ItemPath), MessageType.Warning);


								loadResult = ItemLoadResult.Skipped;
								return null;
							}

							if (options.Preset.Exclude.MatchesPath(path))
							{
								options.Progress.ReportStatus(string.Format("[SKIPPED] {0}:{1} because the preset excluded it by path, but the item was on disk", serializedItem.DatabaseName, serializedItem.ItemPath), MessageType.Warning);

								loadResult = ItemLoadResult.Skipped;
								return null;
							}

							var newOptions = new LoadOptions(options);

							// in some cases we want to force an update for this item only
							if (!options.ForceUpdate && ShouldForceUpdate(serializedItem, options.Progress))
							{
								options.Progress.ReportStatus(string.Format("[FORCED] {0}:{1}", serializedItem.DatabaseName, serializedItem.ItemPath), MessageType.Info);
								newOptions.ForceUpdate = true;
							}

							result = ItemSynchronization.PasteSyncItem(serializedItem, newOptions, true);

							loadResult = ItemLoadResult.Success;
						}
						catch (ParentItemNotFoundException ex)
						{
							result = null;
							loadResult = ItemLoadResult.Error;
							string error =
								"Cannot load item from path '{0}'. Possible reason: parent item with ID '{1}' not found.".FormatWith(
									PathUtils.UnmapItemPath(path, options.Root), ex.ParentID);

							options.Progress.ReportStatus(error, MessageType.Error);

							LogLocalizedError(error);
						}
						catch (ParentForMovedItemNotFoundException ex2)
						{
							result = ex2.Item;
							loadResult = ItemLoadResult.Error;
							string error =
								"Item from path '{0}' cannot be moved to appropriate location. Possible reason: parent item with ID '{1}' not found."
									.FormatWith(PathUtils.UnmapItemPath(path, options.Root), ex2.ParentID);

							options.Progress.ReportStatus(error, MessageType.Error);

							LogLocalizedError(error);
						}
						return result;
					}
					finally
					{
						ItemHandler.DisabledLocally = disabledLocally;
					}
				}
			}

			loadResult = ItemLoadResult.Error;

			return null;
		}

		/// <summary>
		/// Checks to see if we should force an item to be written from the serialized version.
		/// This changes the rules slightly from the default deserializer by forcing if the dates are
		/// different instead of only if they are newer on disk.
		/// </summary>
		private bool ShouldForceUpdate(SyncItem syncItem, IProgressStatus progress)
		{
			Database db = Factory.GetDatabase(syncItem.DatabaseName);

			Assert.IsNotNull(db, "Database was null");

			Item target = db.GetItem(syncItem.ID);

			if (target == null) return true; // target item doesn't exist - must force

			// see if the modified date is different in any version (because disk is master, ANY changes we want to force overwrite)
			return syncItem.Versions.Any(version =>
				{
					Item targetVersion = target.Database.GetItem(target.ID, Language.Parse(version.Language), Sitecore.Data.Version.Parse(version.Version));
					var serializedModified = version.Values[FieldIDs.Updated.ToString()];
					var itemModified = targetVersion[FieldIDs.Updated.ToString()];

					if (!string.IsNullOrEmpty(serializedModified))
					{
						var result = string.Compare(serializedModified, itemModified, StringComparison.InvariantCulture) != 0;


						if (result)
							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk modified {3}, Item modified {4}", syncItem.ItemPath, version.Language,
											version.Version, serializedModified, itemModified), MessageType.Debug);

						return result;
					}

					// ocasionally a version will not have a modified date, only a revision so we compare those as a backup
					var serializedRevision = version.Values[FieldIDs.Revision.ToString()];
					var itemRevision = targetVersion.Statistics.Revision;

					if (!string.IsNullOrEmpty(serializedRevision))
					{
						var result = string.Compare(serializedRevision, itemRevision, StringComparison.InvariantCulture) != 0;

						if (result)
							progress.ReportStatus(
								string.Format("{0} ({1} #{2}): Disk revision {3}, Item revision {4}", syncItem.ItemPath, version.Language,
											version.Version, serializedRevision, itemRevision), MessageType.Debug);

						return result;
					}

					// if we get here we have no valid updated or revision to compare. Let's ignore the item as if it was a real item it'd have one of these.
					if(!syncItem.ItemPath.StartsWith("/sitecore/templates/System") && !syncItem.ItemPath.StartsWith("/sitecore/templates/Sitecore Client")) // this occurs a lot in stock system templates - we ignore warnings for those as it's expected.
						progress.ReportStatus(string.Format("{0} ({1} #{2}): Serialized version had no modified or revision field to check for update.", syncItem.ItemPath, version.Language, version.Version), MessageType.Warning);

					return false;
				});
		}

		/// <summary>
		/// Deletes an item from Sitecore
		/// </summary>
		/// <returns>true if the item's database should have its template engine reloaded, false otherwise</returns>
		private bool DeleteItem(Item item, IProgressStatus progress)
		{
			bool resetFromChild = DeleteItems(item.Children, progress);
			Database db = item.Database;
			ID id = item.ID;
			string path = item.Paths.Path;

			item.Delete();

			if (EventDisabler.IsActive)
			{
				db.Caches.ItemCache.RemoveItem(id);
				db.Caches.DataCache.RemoveItemInformation(id);
			}

			progress.ReportStatus("[DELETED] {0}:{1} because it did not exist on disk".FormatWith(db.Name, path), MessageType.Warning);

			if (!resetFromChild && item.Database.Engines.TemplateEngine.IsTemplatePart(item))
			{
				return true;
			}

			return false;
		}
		/// <summary>
		/// Deletes a list of items. Ensures that obsolete cache data is also removed.
		/// </summary>
		/// <returns>
		/// Is set to <c>true</c> if template engine should reset afterwards.
		/// </returns>
		private bool DeleteItems(IEnumerable<Item> items, IProgressStatus progress)
		{
			bool reset = false;
			foreach (Item item in items)
			{
				if (DeleteItem(item, progress))
					reset = true;
			}

			return reset;
		}
		/// <summary>
		/// Raises the "serialization finished" event.
		/// </summary>
		private void DeserializationFinished(string databaseName)
		{
			EventManager.RaiseEvent(new SerializationFinishedEvent());
			Database database = Factory.GetDatabase(databaseName, false);
			if (database != null)
			{
				database.RemoteEvents.Queue.QueueEvent(new SerializationFinishedEvent());
			}
		}

		private static string GetTargetDatabase(string path, LoadOptions options)
		{
			if (options.Database != null)
			{
				return options.Database.Name;
			}
			string path2 = PathUtils.UnmapItemPath(path, PathUtils.Root);
			ItemReference itemReference = ItemReference.Parse(path2);
			return itemReference.Database;
		}

		/// <summary>
		/// Determines whether [is standard values item] [the specified file name].
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>
		///   <c>true</c> if [is standard values item] [the specified file name]; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsStandardValuesItem(string fileName, out ID itemId)
		{
			Assert.ArgumentNotNull(fileName, "fileName");
			string itemPath;
			try
			{
				using (TextReader textReader = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
				{
					var item = SyncItem.ReadItem(new Tokenizer(textReader));

					itemId = ID.Parse(item.ID);
					itemPath = item.ItemPath;
				}
			}
			catch
			{
				itemId = null;
				return false;
			}

			string[] array = itemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length > 0)
			{
				if (array.Any(s => s.Equals("templates", StringComparison.InvariantCultureIgnoreCase)))
				{
					return array.Last().Equals("__Standard Values");
				}
			}

			return false;
		}

		/// <summary>
		/// Logs localized strings.
		/// </summary>
		private static void LogLocalized(string message, params object[] parameters)
		{
			Assert.IsNotNullOrEmpty(message, "message");
			Job job = Context.Job;
			if (job != null)
			{
				job.Status.LogInfo(message, parameters);
				return;
			}
			Log.Info(message.FormatWith(parameters), new object());
		}
		/// <summary>
		/// Logs localized strings.
		/// </summary>
		private static void LogLocalizedError(string message, params object[] parameters)
		{
			Assert.IsNotNullOrEmpty(message, "message");
			Job job = Context.Job;
			if (job != null)
			{
				job.Status.LogError(message.FormatWith(parameters));
				return;
			}
			Log.Error(message.FormatWith(parameters), new object());
		}

		private class StandardValuesException : Exception
		{
			public StandardValuesException(string itemPath)
				: base(itemPath)
			{
				Assert.ArgumentNotNull(itemPath, "itemPath");
			}

			public override string ToString()
			{
				return "Reverting of Standard values of template is delayed. " + this.Message;
			}
		}

		/// <summary>
		/// Represents a single failure in a recursive directory scan operation
		/// </summary>
		private class Failure
		{
			/// <summary>
			/// The directory.
			/// </summary>
			private readonly string _directory;
			/// <summary>
			/// The reason.
			/// </summary>
			private readonly Exception _reason;
			/// <summary>
			/// Gets the directory.
			/// </summary>
			/// <value>The directory.</value>
			public string Directory
			{
				get
				{
					return _directory;
				}
			}
			/// <summary>
			/// Gets the reason.
			/// </summary>
			/// <value>The reason.</value>
			public Exception Reason
			{
				get
				{
					return _reason;
				}
			}
			/// <summary>
			/// Initializes a new instance of the <see cref="T:Sitecore.Data.Serialization.Manager.Failure" /> class.
			/// </summary>
			/// <param name="directory">
			/// The directory.
			/// </param>
			/// <param name="reason">
			/// The reason.
			/// </param>
			public Failure(string directory, Exception reason)
			{
				Assert.ArgumentNotNullOrEmpty(directory, "directory");
				Assert.ArgumentNotNull(reason, "reason");
				_directory = directory;
				_reason = reason;
			}
		}

		/// <summary>
		/// The result from loading a single item from disk
		/// </summary>
		private enum ItemLoadResult { Success, Error, Skipped }

	}
}
