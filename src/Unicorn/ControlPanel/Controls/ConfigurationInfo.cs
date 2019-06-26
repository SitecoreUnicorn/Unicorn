using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Sitecore.ContentSearch.Utilities;
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
			var configurationHasInvalidPredicates = ControlPanelUtility.GetInvalidRootPaths(_configuration).Length > 0;

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
				var s = string.Join(",", dependents.Select(d => d.Configuration.Name).ToArray());
				writer.Write($@"
					<span class=""badge"" title=""This configuration depends on {dependents.Length} other configuration(s) => {s}"">Dep: {dependents.Length}</span>");
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

			if (!configurationHasValidRootPathParents)
			{
				writer.Write(@"
					<p class=""warning"">This configuration's predicate cannot resolve all root items. This usually means the predicate is configured to rely on a parent item that does not exist in the Sitecore database.<br /><br />");
				writer.Write(@"The following predicates rely on a missing root path: <br />");
				foreach (string r in ControlPanelUtility.GetInvalidRootPaths(_configuration))
				{
					writer.Write($"- {r}<br />");
				}
				writer.Write("</p>");
			}
			else if (!configurationHasAnySerializedItems && !configurationHasInvalidPredicates)
			{
				writer.Write(@"
					<p class=""warning"">This configuration does not currently have any valid serialized items. You cannot sync it until you perform an initial serialization, which will write the current state of Sitecore to serialized items.</p>");
			}

			var configDetails = _configuration.Resolve<ConfigurationDetails>();
			configDetails.ConfigurationName = _configuration.Name;

			if (!configurationHasAnySerializedItems && configurationHasValidRootPathParents)
			{
				configDetails.Render(writer);
				new InitialSetup(_configuration).Render(writer);
			}
			else if (configurationHasValidRootPathParents)
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

				if (!configurationHasInvalidPredicates)
				{
					writer.Write(@"
						<a class=""button"" data-basehref=""?verb=Reserialize&amp;configuration={0}"" href=""?verb=Reserialize&amp;configuration={0}"" onclick=""return confirm('{1}')"">Reserialize</a>", htmlConfigName, blurb);
				}

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