using System;
using System.Web;
using System.Web.UI;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
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
			var configurationHasSerializedItems = ControlPanelUtility.HasAnySerializedItems(_configuration);
			var configurationHasValidRootItems = ControlPanelUtility.HasAnySourceItems(_configuration);
			var modalId = "m" + Guid.NewGuid();

			writer.Write(@"
			<tr>
				<td{0}>", configurationHasSerializedItems ? string.Empty:" colspan=\"2\"");

			writer.Write(@"
					<h3{0}{1}</h3>".FormatWith(MultipleConfigurationsExist ? @" class=""fakebox""><span></span>" : ">", _configuration.Name));

			if(!string.IsNullOrWhiteSpace(_configuration.Description))
				writer.Write(@"
					<p>{0}</p>", _configuration.Description);

			if (configurationHasSerializedItems)
			{
				writer.Write(@"
					<p><a href=""#"" data-modal=""{0}"" class=""info"">Detailed configuration information</a></p>", modalId);
			}

			writer.Write(@"
					</p>");

			if (!configurationHasValidRootItems)
				writer.Write(@"
					<p class=""warning"">This configuration's predicate cannot resolve any valid root items. This usually means it is configured to look for nonexistent paths or GUIDs. Please review your predicate configuration.</p>");
			else if (!configurationHasSerializedItems)
				writer.Write(@"
					<p class=""warning"">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization.</p>");

			var dpConfig = _configuration.Resolve<IUnicornDataProviderConfiguration>();
			if (dpConfig != null && dpConfig.EnableTransparentSync) writer.Write(@"
					<p class=""transparent-sync"">Transparent sync is enabled for this configuration.</p>");

			var configDetails = _configuration.Resolve<ConfigurationDetails>();
			configDetails.ConfigurationName = _configuration.Name;
			configDetails.ModalId = configurationHasSerializedItems && configurationHasValidRootItems ? modalId : null;
			configDetails.Render(writer);

			if (!configurationHasSerializedItems && configurationHasValidRootItems)
			{
				new InitialSetup(_configuration).Render(writer);
			}

			if (configurationHasSerializedItems)
			{
				writer.Write(@"
				</td>
				<td class=""controls"">");

				var htmlConfigName = HttpUtility.UrlEncode(_configuration.Name ?? string.Empty);

				writer.Write(@"
					<a class=""button"" href=""?verb=Reserialize&amp;configuration={0}"" onclick=""return confirm('This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?')"">Reserialize</a>", htmlConfigName);

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
