using System.Collections.Generic;
using System.Text;
using Rainbow;
using Rainbow.Model;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Unicorn.Configuration;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.UI.Pipelines.GetContentEditorWarnings;

namespace Unicorn.Evaluators
{
	/// <summary>
	/// Evaluates to add new items only. Existing or orphaned items are left alone.
	/// NOTE: Using transparent sync with this evaluator is not a good idea, because the semantics of transparent sync are to read from the serialization store directly.
	/// In other words, transparent sync always acts like SerializedAsMasterEvaluator, and because there is no sync invocation the evaluator is not invoked.
	/// </summary>
	public class NewItemOnlyEvaluator : IEvaluator, IDocumentable
	{
		private readonly INewItemOnlyEvaluatorLogger _logger;
		private readonly ISourceDataStore _sourceDataStore;
		private readonly ITargetDataStore _targetDataStore;
		private readonly IConfiguration _parentConfiguration;
		protected readonly bool IsDevMode = Settings.GetBoolSetting("Unicorn.DevMode", true);

		public NewItemOnlyEvaluator(INewItemOnlyEvaluatorLogger logger, ISourceDataStore sourceDataStore, ITargetDataStore targetDataStore, IConfiguration parentConfiguration)
		{
			Assert.ArgumentNotNull(logger, "logger");
			Assert.ArgumentNotNull(sourceDataStore, "sourceDataStore");
			Assert.ArgumentNotNull(targetDataStore, "targetDataStore");
			Assert.ArgumentNotNull(parentConfiguration, "parentConfiguration");

			_logger = logger;
			_sourceDataStore = sourceDataStore;
			_targetDataStore = targetDataStore;
			_parentConfiguration = parentConfiguration;
		}

		public virtual void EvaluateOrphans(IItemData[] orphanItems)
		{
			Assert.ArgumentNotNull(orphanItems, "orphanItems");
			foreach(var orphan in orphanItems) _logger.Evaluated(orphan);
		}

		public virtual IItemData EvaluateNewSerializedItem(IItemData newItemData)
		{
			Assert.ArgumentNotNull(newItemData, "newItem");

			_logger.DeserializedNewItem(newItemData);

			_sourceDataStore.Save(newItemData);

			_logger.Evaluated(newItemData);

			return newItemData;
		}

		public virtual IItemData EvaluateUpdate(IItemData sourceItem, IItemData targetItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItemData");
			_logger.Evaluated(sourceItem);

			return null;
		}

		public virtual Warning EvaluateEditorWarning(Item item, PredicateResult predicateResult)
		{
			var existingTargetItem = _targetDataStore.GetByPathAndId(item.Paths.Path, item.ID.Guid, item.Database.Name);

			// if we have no existing serialized item, there's no need for a warning: Unicorn won't touch this item when using NIO
			if (existingTargetItem == null) return null;

			var title = "This item is part of a Unicorn deploy once configuration.";
			var message = new StringBuilder();

			// if dev mode is on, we don't need a warning
			if (IsDevMode)
			{
				message.Append("Changes to this item will not be synced to other environments unless the item does not exist yet.");
			}
			else
			{
				title = "This item was added by developers.";
				message.Append("You may edit this item, but <b>it will return next time code is deployed</b>. Ask a developer to help if you need to delete this item.");
			}

			message.Append($"<br><br><b>Configuration</b>: {_parentConfiguration.Name}");
			if (predicateResult.PredicateComponentId != null)
			{
				message.Append($"<br><b>Predicate Component</b>: {predicateResult.PredicateComponentId}");
			}

			// check if serialized item ID looks like a filesystem path e.g. c:\
			if (IsDevMode && existingTargetItem?.SerializedItemId != null && existingTargetItem.SerializedItemId.Substring(1, 2) == ":\\")
			{
				message.Append($"<br><b>Physical path</b>: <span style=\"font-family: consolas, monospace\">{existingTargetItem.SerializedItemId}</span>");
			}

			return new Warning(title, message.ToString());
		}

		public virtual bool ShouldPerformConflictCheck(Item item)
		{
			// we don't care about conflicts because items may be edited after creation and are expected to not necessarily match serialized versions
			return false;
		}

		public virtual string FriendlyName => "New Item Only Evaluator";

		public virtual string Description => "During a sync only items that are not already in the Sitecore database are synced. If an item already exists, it is never modified. Useful for deploying items only once and leaving them alone from then on.";

		public virtual KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			return new KeyValuePair<string, string>[0];
		}
	}
}
