using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.StringExtensions;
using Unicorn.Configuration;
using Unicorn.Configuration.Dependencies;
using Unicorn.Data;
using Unicorn.Data.DataProvider;
using Unicorn.Data.Dilithium;

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
			var configurationHasValidRootPathParents = ControlPanelUtility.AllRootParentPathsExist(_configuration);
			var dependents = _configuration.Resolve<ConfigurationDependencyResolver>().Dependencies;

			var modalId = "m" + Guid.NewGuid();

			writer.Write(@"
			<tr>
				<td{0}>", configurationHasAnySerializedItems ? string.Empty : " colspan=\"2\"");

			writer.Write(@"
					<h3{0}{1}</h3>".FormatWith(MultipleConfigurationsExist && configurationHasAnySerializedItems && configurationHasValidRootPathParents ? @" class=""fakebox""><span></span>" : ">", _configuration.Name));

			if (!string.IsNullOrWhiteSpace(_configuration.Description))
				writer.Write(@"
					<p>{0}</p>", _configuration.Description);

			if (configurationHasAnySerializedItems)
			{
				writer.Write(@"
					<span class=""badge""><a href=""#"" data-modal=""{0}"" class=""info"">Show Config</a></span>", modalId);
			}

			// Transparent Sync badge
			var dpConfig = _configuration.Resolve<IUnicornDataProviderConfiguration>();
			if (dpConfig != null && dpConfig.EnableTransparentSync)
				writer.Write(@"
					<span class=""badge"" title=""Transparent Sync"">TS</span>");

			// Dependent configs badge
			if (dependents.Any())
			{
				writer.Write($@"
					<span class=""badge"" title=""{dependents.Length} other configuration(s) depend on items in this configuration."">Dep: {dependents.Length}</span>");
			}

			// Dilithium badge
			var diSql = _configuration.EnablesDilithiumSql();
			var diSfs = _configuration.EnablesDilithiumSfs();
			if (diSql || diSfs)
			{
				var diState = diSql && diSfs ? "Full" : (diSql ? "SQL" : "Serialized");

				writer.Write($@"
					<span class=""badge"" 
						title=""Uses Dilithium high speed cached data stores.{(diSql ? " Direct SQL active." : string.Empty)}{(diSfs ? " Serialized snapshots active." : string.Empty)}"">
						Dilithium: {diState}
					</span>");
			}

			if (!configurationHasValidRootPathParents && !configurationHasAnySerializedItems)
				writer.Write(@"
					<p class=""warning"">This configuration's predicate cannot resolve any valid root items. This usually means the predicate is configured to include paths that do not exist in the Sitecore database.</p>");
			else if (!configurationHasAnySerializedItems)
				writer.Write(@"
					<p class=""warning"">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization, which will write the current state of Sitecore to serialized items.</p>");

			var configDetails = _configuration.Resolve<ConfigurationDetails>();
			configDetails.ConfigurationName = _configuration.Name;

			if (!configurationHasAnySerializedItems)
			{
				configDetails.Render(writer);
				new InitialSetup(_configuration).Render(writer);
			}
			else
			{
				configDetails.ModalId = modalId;
				configDetails.Render(writer);

				writer.Write(@"
				</td>
				<td class=""controls"">");

				var htmlConfigName = HttpUtility.UrlEncode(_configuration.Name ?? string.Empty);

				var blurb = _configuration.Resolve<IUnicornDataProviderConfiguration>().EnableTransparentSync
					? "DANGER: This configuration uses Transparent Sync. Items may not actually exist in the database, and if they do not reserializing will DELETE THEM from serialized. Continue?"
					: "This will reset the serialized state to match Sitecore. This normally is not needed after initial setup unless changing path configuration. Continue?";

				writer.Write(@"
					<a class=""button"" data-basehref=""?verb=Reserialize&amp;configuration={0}"" href=""?verb=Reserialize&amp;configuration={0}"" onclick=""return confirm('{1}')"">Reserialize</a>", htmlConfigName, blurb);

				writer.Write(@"
					<a class=""button"" data-basehref=""?verb=Sync&amp;configuration={0}"" href=""?verb=Sync&amp;configuration={0}"">Sync</a>", htmlConfigName);
			}

			writer.Write(@"
				</td>");

			writer.Write(@"
			</tr>");
		}
	}
}