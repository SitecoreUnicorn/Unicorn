using Unicorn.Data;
using Unicorn.Evaluators;
using Unicorn.Logging;
using Unicorn.Serialization;

namespace Unicorn.Publishing
{
	public class AutoPublishSerializedAsMasterEvaluatorLogger : DefaultSerializedAsMasterEvaluatorLogger
	{
		public AutoPublishSerializedAsMasterEvaluatorLogger(ILogger logger) : base(logger)
		{
		
		}

		public override void DeserializedNewItem(ISerializedItem serializedItem)
		{
			base.DeserializedNewItem(serializedItem);
			ManualPublishQueueHandler.QueueSerializedItem(serializedItem);
		}

		public override void SerializedUpdatedItem(ISerializedItem serializedItem)
		{
			base.SerializedUpdatedItem(serializedItem);
			ManualPublishQueueHandler.QueueSerializedItem(serializedItem);
		}

		public override void DeletedItem(ISourceItem deletedItem)
		{
			base.DeletedItem(deletedItem);
			ManualPublishQueueHandler.QueueSourceItem(deletedItem);
		}
	}
}
