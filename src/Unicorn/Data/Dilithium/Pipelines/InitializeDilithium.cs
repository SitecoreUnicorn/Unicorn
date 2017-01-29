using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Unicorn.Data.Dilithium.Rainbow;
using Unicorn.Data.Dilithium.Sql;
using Unicorn.Pipelines.UnicornOperationStart;

namespace Unicorn.Data.Dilithium.Pipelines
{
	public class InitializeDilithium : IUnicornOperationStartProcessor
	{
		public void Process(UnicornOperationStartPipelineArgs args)
		{
			args.Logger.Info($"Dilithium is precaching items in {args.Configurations.Length} configurations.");

			var sw = new Stopwatch();
			sw.Start();

			if (ReactorContext.IsActive)
			{
				args.Logger.Warn("Dilithium cache context was active, which probably means another Unicorn operation is currently running.");
				args.Logger.Warn("In order to prevent corruption of both operations, we are disabling Dilithium for both operations instead to ensure consistency.");
				ReactorContext.Dispose();
				return;
			}

			var sourceItems = Task.Run(() =>
			{
				using (new UnicornOperationContext())
				{
					var sqlPrecache = new SqlPrecacheStore(args.Configurations);

					var initData = args.PartialOperationRoot != null ? sqlPrecache.Initialize(false, args.PartialOperationRoot) : sqlPrecache.Initialize(false);

					if (!initData.LoadedItems)
					{
						args.Logger.Debug("No current configurations enabled DilithiumSitecoreDataStore. Sitecore APIs will be used.");
					}
					else
					{
						args.Logger.Debug($"[SQL] Batch precached {initData.TotalLoadedItems} total DB items in {initData.LoadTimeMsec} ms");
					}

					if (initData.FoundConsistencyErrors)
					{
						args.Logger.Warn("Detected field storage corruption in the Sitecore database. See the Sitecore logs for details.");
					}

					ReactorContext.SqlPrecache = sqlPrecache;

					return initData.TotalLoadedItems;
				}
			});


			var targetItems = Task.Run(() =>
			{
				// there's little point to snapshotting when we're doing a partial sync/partial reserialize
				// as the tree is probably quite a small chunk of the whole. So we skip out here.
				if (args.PartialOperationRoot != null) return 0;

				using (new UnicornOperationContext())
				{
					var rainbowPrecache = new RainbowPrecacheStore(args.Configurations);

					var initData = rainbowPrecache.Initialize(false);

					if (!initData.LoadedItems)
					{
						args.Logger.Debug("No current configurations enabled DilithiumSfsDataStore. Rainbow APIs will be used.");
					}
					else
					{
						args.Logger.Debug($"[SFS] Batch precached {initData.TotalLoadedItems} total serialized items in {initData.LoadTimeMsec} ms");
					}

					ReactorContext.RainbowPrecache = rainbowPrecache;

					return initData.TotalLoadedItems;
				}
			});

			Task.WaitAll(sourceItems, targetItems);

			sw.Stop();

			var totalItems = sourceItems.Result + targetItems.Result;

			var msPerItem = (sw.ElapsedMilliseconds / (double)Math.Max(1, totalItems)).ToString("N2");

			args.Logger.Info($"Dilithium init completed in {sw.ElapsedMilliseconds}ms ({totalItems} items; ~{msPerItem}ms/item)");
			args.Logger.Debug(string.Empty);
		}
	}
}
