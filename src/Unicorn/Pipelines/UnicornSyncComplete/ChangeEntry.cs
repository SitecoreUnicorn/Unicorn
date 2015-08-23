using System;
using Rainbow.Model;

namespace Unicorn.Pipelines.UnicornSyncComplete
{
	public class ChangeEntry
	{
		public ChangeEntry(IItemData itemData, ChangeType type)
		{
			if (itemData != null)
			{
				Id = itemData.Id;
				DatabaseName = itemData.DatabaseName;
			}
			
			ChangeType = type;
		}

		public Guid? Id { get; private set; }
		public string DatabaseName { get; private set; }
		public ChangeType ChangeType { get; private set; }
	}
}
