using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unicorn.Data.Dilithium.Rainbow;
using Unicorn.Data.Dilithium.Sql;
using Unicorn.Pipelines;
using Unicorn.Pipelines.UnicornReserializeStart;
using Unicorn.Pipelines.UnicornSyncStart;

namespace Unicorn.Data.Dilithium.Pipelines
{
	public class InitializeDilithium : IUnicornSyncStartProcessor
	{
		public void Process(UnicornReserializeStartPipelineArgs args)
		{
			ProcessInternal(args);
		}

		public void Process(UnicornSyncStartPipelineArgs args)
		{
			ProcessInternal(args);
		}

		protected virtual void ProcessInternal(IUnicornOperationStartPipelineArgs args)
		{
			var configCount = args.Configurations.Count(config => config.EnablesDilithium());

			if (configCount == 0)
			{
				args.Logger.Info("No current configurations enabled Dilithium. Skipping precache.");
				return;
			}

			args.Logger.Info($"Precaching items in {configCount} Dilithium-enabled configuration(s).");

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
						args.Logger.Debug("[SQL] No current configurations enabled Dilithium SQL. Precache disabled.");
					}
					else
					{
						args.Logger.Debug($"[SQL] Batch precached {initData.TotalLoadedItems} total DB items in {initData.LoadTimeMsec} ms");
					}

					if (initData.FoundConsistencyErrors)
					{
						args.Logger.Warn("[SQL] Detected field storage corruption in the Sitecore database. See the Sitecore logs for details.");
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
						args.Logger.Debug("[Serialized] No current configurations enabled Dilithium Serialized. Precache disabled.");
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
