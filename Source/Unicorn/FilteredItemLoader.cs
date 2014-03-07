using System;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Publishing;
using Sitecore.SecurityModel;

namespace Unicorn
{
    public class FilteredItemLoader
    {
        public void Process(PipelineArgs args)
        {
            var syncDatabase = Settings.GetBoolSetting("UnicornSyncDatabase", false);
            if (!syncDatabase)
            {
                Log.Warn("Unicorn automatic sync to database is disabled.", this);
                return;
            }

            // load the requested (or default) preset
            var presets = SerializationUtility.GetPreset("default").ToList();
            if (!presets.Any())
            {
                Log.Warn("Unicorn preset did not exist in configuration.", this);
                return;
            }

            presets.ForEach(ProcessPreset);

            var publishDatabase = Settings.GetBoolSetting("UnicornPublishDatabase", false);
            if (!publishDatabase)
            {
                return;
            }

            presets.ForEach(PublishPreset);
        }

        private void ProcessPreset(IncludeEntry preset)
        {
            var options = new AdvancedLoadOptions(preset)
            {
                ForceUpdate = false,
                DeleteOrphans = true
            };

            try
            {
                using (new SecurityDisabler())
                {
                    new SerializationLoader().LoadTree(options);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unicorn unable to process preset", ex, this);
                throw;
            }
        }

        private void PublishPreset(IncludeEntry preset)
        {
            if (preset.Database != "master")
            {
                return;
            }

            var master = Factory.GetDatabase("master");
            var target = Factory.GetDatabase("web");
            var home = master.GetItem(preset.Path);
            Database[] targetDatabases = { target };
            try
            {
                PublishManager.PublishItem(home, targetDatabases, master.Languages, true, true);
            }
            catch (Exception ex)
            {
                Log.Error("Unicorn unable to publish preset", ex, this);
                throw;
            }
        }
    }
}