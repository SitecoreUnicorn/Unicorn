#region using

using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Data.DataProvider;

#endregion

namespace Unicorn.ControlPanel.Controls
{
    internal class ConfigurationInfo : IControlPanelControl
    {
        public IConfiguration Configuration { get; }

        public bool MultipleConfigurationsExist { get; set; }

        public ConfigurationInfo(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Render(HtmlTextWriter writer)
        {
            var configurationHasAnySerializedItems = ControlPanelUtility.HasAnySerializedItems(Configuration);
            var configurationHasValidRootPaths = ControlPanelUtility.AllRootPathsExists(Configuration);
            var configurationHasDependants = ControlPanelUtility.HasDependants(Configuration);

            var modalId = "m" + Guid.NewGuid();

            writer.Write(@"<tr><td{0}>", configurationHasAnySerializedItems ? string.Empty : " colspan=\"2\"");

            writer.Write(@"<h3{0}{1}</h3>".FormatWith(MultipleConfigurationsExist && configurationHasAnySerializedItems && configurationHasValidRootPaths ? @" class=""fakebox""><span></span>" : ">", Configuration.Name));

            if (!string.IsNullOrWhiteSpace(Configuration.Description))
                writer.Write(@"<p>{0}</p>", Configuration.Description);

            if (configurationHasAnySerializedItems)
                writer.Write(@"<p><a href=""#"" data-modal=""{0}"" class=""info"">Detailed configuration information</a></p>", modalId);
            if (configurationHasDependants)
            {
                writer.Write(@"<div class=""help"">");
                writer.Write(@"<p>The following configurations are dependant on this configuration:</p>");
                writer.Write(@"<ul>");
                foreach (var dependant in ControlPanelUtility.FindConfigurationsDependants(Configuration))
                {
                    writer.Write($@"<li>{dependant.Name}</li>");
                }
                writer.Write(@"</ul>");
                writer.Write(@"<p>Please sync this configuration before the dependants.</p>");
                writer.Write(@"</div>");
            }

            if (!configurationHasValidRootPaths)
                writer.Write(@"<p class=""warning"">This configuration's predicate cannot resolve any valid root items. This usually means it is configured to look for nonexistent paths or GUIDs. Please review your predicate configuration.</p>");
            else if (!configurationHasAnySerializedItems)
                writer.Write(@"<p class=""warning"">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization.</p>");

            var dpConfig = Configuration.Resolve<IUnicornDataProviderConfiguration>();
            if (dpConfig != null && dpConfig.EnableTransparentSync)
                writer.Write(@"<p class=""transparent-sync"">Transparent sync is enabled for this configuration.</p>");

            var configDetails = Configuration.Resolve<ConfigurationDetails>();
            configDetails.ConfigurationName = Configuration.Name;
            configDetails.ModalId = modalId;
            configDetails.Render(writer);

            if (!configurationHasAnySerializedItems)
                new InitialSetup(Configuration).Render(writer);
            else
            {
                writer.Write(@"</td><td class=""controls"">");

                var htmlConfigName = HttpUtility.UrlEncode(Configuration.Name ?? string.Empty);

                var blurb = Configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync ? "DANGER: This configuration uses Transparent Sync. Items may not actually exist in the database, and if they do not reserializing will DELETE THEM from serialized. Continue?" : "This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?";

                writer.Write(@"<a class=""button"" href=""?verb=Reserialize&amp;configuration={0}"" onclick=""return confirm('{1}')"">Reserialize</a>", htmlConfigName, blurb);
                writer.Write(@"&nbsp;");
                writer.Write(@"<a class=""button"" href=""?verb=Sync&amp;configuration={0}"">Sync</a>", htmlConfigName);
            }

            writer.Write(@"</td>");

            writer.Write(@"</tr>");
        }
    }
}