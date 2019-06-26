namespace Unicorn.Data.Dilithium
{
	public class InitResult
	{
		public InitResult(bool loadedItems)
		{
			LoadedItems = loadedItems;
		}

		public InitResult(bool loadedItems, bool foundConsistencyErrors, int totalLoadedItems, int loadTimeMsec)
		{
			LoadedItems = loadedItems;
			FoundConsistencyErrors = foundConsistencyErrors;
			TotalLoadedItems = totalLoadedItems;
			LoadTimeMsec = loadTimeMsec;
		}

		public bool LoadedItems { get; }

		public bool FoundConsistencyErrors { get; }

		public int TotalLoadedItems { get; }

		public int LoadTimeMsec { get; }
	}
}
