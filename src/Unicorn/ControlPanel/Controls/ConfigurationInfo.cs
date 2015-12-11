using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.Data.DataProvider;

namespace Unicorn.ControlPanel.Controls
{
	internal class ConfigurationInfo : IControlPanelControl
	{
		private readonly IConfiguration _configuration;

		public bool MultipleConfigurationsExist { get; set; }

		public ConfigurationInfo(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public void Render(HtmlTextWriter writer)
		{
			var configurationHasAnySerializedItems = ControlPanelUtility.HasAnySerializedItems(_configuration);
			var configurationHasValidRootPaths = ControlPanelUtility.AllRootPathsExists(_configuration);
			var dependents = _configuration.Resolve<ConfigurationDependencyResolver>().Dependencies;

			var modalId = "m" + Guid.NewGuid();

			writer.Write(@"
			<tr>
				<td{0}>", configurationHasAnySerializedItems ? string.Empty : " colspan=\"2\"");

			writer.Write(@"
					<h3{0}{1}</h3>".FormatWith(MultipleConfigurationsExist && configurationHasAnySerializedItems && configurationHasValidRootPaths ? @" class=""fakebox""><span></span>" : ">", _configuration.Name));

			if (!string.IsNullOrWhiteSpace(_configuration.Description))
				writer.Write(@"
					<p>{0}</p>", _configuration.Description);

			if (dependents.Any())
			{
				writer.Write(@"
					<p class=""help"">This configuration depends on {0}, which should sync before it.</p>", string.Join(", ", dependents.Select(dep => dep.Configuration.Name)));
			}

			if (configurationHasAnySerializedItems)
			{
				writer.Write(@"
					<p><a href=""#"" data-modal=""{0}"" class=""info"">Detailed configuration information</a></p>", modalId);
			}

			if (!configurationHasValidRootPaths)
				writer.Write(@"
					<p class=""warning"">This configuration's predicate cannot resolve any valid root items. This usually means it is configured to look for nonexistent paths or GUIDs. Please review your predicate configuration.</p>");
			else if (!configurationHasAnySerializedItems)
				writer.Write(@"
					<p class=""warning"">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization.</p>");

			var dpConfig = _configuration.Resolve<IUnicornDataProviderConfiguration>();
			if (dpConfig != null && dpConfig.EnableTransparentSync)
				writer.Write(@"
					<p class=""transparent-sync"">Transparent sync is enabled for this configuration.</p>");

			var configDetails = _configuration.Resolve<ConfigurationDetails>();
			configDetails.ConfigurationName = _configuration.Name;
			configDetails.ModalId = modalId;
			configDetails.Render(writer);

			if (!configurationHasAnySerializedItems)
				new InitialSetup(_configuration).Render(writer);
			else
			{
				writer.Write(@"
				</td>
				<td class=""controls"">");

				var htmlConfigName = HttpUtility.UrlEncode(_configuration.Name ?? string.Empty);

				var blurb = _configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync 
					? "DANGER: This configuration uses Transparent Sync. Items may not actually exist in the database, and if they do not reserializing will DELETE THEM from serialized. Continue?"
					: "This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?";

				writer.Write(@"
					<a class=""button"" href=""?verb=Reserialize&amp;configuration={0}"" onclick=""return confirm('{1}')"">Reserialize</a>", htmlConfigName, blurb);

				writer.Write(@"
					<a class=""button"" href=""?verb=Sync&amp;configuration={0}"">Sync</a>", htmlConfigName);
			}

			writer.Write(@"
				</td>");

			writer.Write(@"
			</tr>");
		}
	}
}