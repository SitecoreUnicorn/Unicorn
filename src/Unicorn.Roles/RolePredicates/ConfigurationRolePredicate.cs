using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;
using Unicorn.Predicates;

namespace Unicorn.Roles.RolePredicates
{
	public class ConfigurationRolePredicate : IRolePredicate
	{
		private readonly IList<ConfigurationRolePredicateEntry> _includeEntries;

		public ConfigurationRolePredicate(XmlNode configNode)
		{
			Assert.ArgumentNotNull(configNode, nameof(configNode));

			_includeEntries = ParseConfiguration(configNode);
		}
		public PredicateResult Includes(Role role)
		{
			Assert.ArgumentNotNull(role, nameof(role));

			// no entries = include everything
			if (_includeEntries.Count == 0) return new PredicateResult(true);

			var result = new PredicateResult(true);

			PredicateResult priorityResult = null;

			foreach (var entry in _includeEntries)
			{
				result = Includes(entry, role);

				if (result.IsIncluded) return result; // it's definitely included if anything includes it
				if (!string.IsNullOrEmpty(result.Justification)) priorityResult = result; // a justification means this is probably a more 'important' fail than others
			}

			return priorityResult ?? result; // return the last failure
		}

		/// <summary>
		/// Checks if a preset includes a given item
		/// </summary>
		protected PredicateResult Includes(ConfigurationRolePredicateEntry entry, Role role)
		{
			// domain match
			if(role.Domain == null || !role.Domain.Name.Equals(entry.Domain, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

			// pattern match
			if(!string.IsNullOrWhiteSpace(entry.Pattern) && !Regex.IsMatch(role.Name.Split('\\').Last(), entry.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)) return new PredicateResult(false);
			
			// pattern is either null or white space, or it matches
			return new PredicateResult(true);
		}

		[ExcludeFromCodeCoverage]
		public string FriendlyName => "Configuration Role Predicate";

		[ExcludeFromCodeCoverage]
		public string Description => "Includes security roles into Unicorn syncs by XML configuration.";

		[ExcludeFromCodeCoverage]
		public KeyValuePair<string, string>[] GetConfigurationDetails()
		{
			var configs = new Collection<KeyValuePair<string, string>>();
			foreach (var entry in _includeEntries)
			{
				configs.Add(new KeyValuePair<string, string>(entry.Domain, entry.Pattern));
			}

			return configs.ToArray();
		}

		private IList<ConfigurationRolePredicateEntry> ParseConfiguration(XmlNode configuration)
		{
			var presets = configuration.ChildNodes
				.Cast<XmlNode>()
				.Where(node => node.Name == "include")
				.Select(CreateIncludeEntry)
				.ToList();

			return presets;
		}

		private static ConfigurationRolePredicateEntry CreateIncludeEntry(XmlNode node)
		{
			Assert.ArgumentNotNull(node, nameof(node));

			var result = new ConfigurationRolePredicateEntry(node?.Attributes?["domain"]?.Value);

			result.Pattern = node?.Attributes?["pattern"]?.Value;

			return result;
		}

		public class ConfigurationRolePredicateEntry
		{
			public ConfigurationRolePredicateEntry(string domain)
			{
				Assert.ArgumentNotNullOrEmpty(domain, nameof(domain));

				Domain = domain;
			}

			public string Domain { get; set; }
			public string Pattern { get; set; }
		}
	}
}