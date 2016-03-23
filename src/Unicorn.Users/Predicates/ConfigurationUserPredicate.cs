namespace Unicorn.Users.Predicates
{
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Xml;
  using Sitecore.Diagnostics;
  using Sitecore.Security.Accounts;
  using Unicorn.Predicates;

  public class ConfigurationUserPredicate : IUserPredicate
  {

    private readonly IList<ConfigurationUserPredicateEntry> _includeEntries;

    public ConfigurationUserPredicate(XmlNode configNode)
    {
      Assert.ArgumentNotNull(configNode, nameof(configNode));

      this._includeEntries = this.ParseConfiguration(configNode);
    }
    public PredicateResult Includes(User user)
    {
      Assert.ArgumentNotNull(user, nameof(user));

      // no entries = include everything
      if (this._includeEntries.Count == 0) return new PredicateResult(true);

      var result = new PredicateResult(true);

      PredicateResult priorityResult = null;

      foreach (var entry in this._includeEntries)
      {
        result = this.Includes(entry, user);

        if (result.IsIncluded) return result; // it's definitely included if anything includes it
        if (!string.IsNullOrEmpty(result.Justification)) priorityResult = result; // a justification means this is probably a more 'important' fail than others
      }

      return priorityResult ?? result; // return the last failure
    }

    /// <summary>
    /// Checks if a preset includes a given item
    /// </summary>
    protected PredicateResult Includes(ConfigurationUserPredicateEntry entry, User user)
    {
      // domain match
      if (user.Domain == null || !user.Domain.Name.Equals(entry.Domain, StringComparison.OrdinalIgnoreCase)) return new PredicateResult(false);

      // pattern match
      //if (!string.IsNullOrWhiteSpace(entry.Pattern) && !Regex.IsMatch(role.Name.Split('\\').Last(), entry.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)) return new PredicateResult(false);

      // pattern is either null or white space, or it matches
      return new PredicateResult(true);
    }

    [ExcludeFromCodeCoverage]
    public string FriendlyName => "Configuration User Predicate";

    [ExcludeFromCodeCoverage]
    public string Description => "Includes security users into Unicorn syncs by XML configuration.";

    [ExcludeFromCodeCoverage]
    public KeyValuePair<string, string>[] GetConfigurationDetails()
    {
      var configs = new Collection<KeyValuePair<string, string>>();
      foreach (var entry in this._includeEntries)
      {
        configs.Add(new KeyValuePair<string, string>(entry.Domain, entry.Pattern));
      }

      return configs.ToArray();
    }

    private IList<ConfigurationUserPredicateEntry> ParseConfiguration(XmlNode configuration)
    {
      var presets = configuration.ChildNodes
        .Cast<XmlNode>()
        .Where(node => node.Name == "include")
        .Select(CreateIncludeEntry)
        .ToList();

      return presets;
    }

    private static ConfigurationUserPredicateEntry CreateIncludeEntry(XmlNode node)
    {
      Assert.ArgumentNotNull(node, nameof(node));

      var result = new ConfigurationUserPredicateEntry(node?.Attributes?["domain"]?.Value)
      {
        Pattern = node?.Attributes?["pattern"]?.Value
      };


      return result;
    }

    public class ConfigurationUserPredicateEntry
    {
      public ConfigurationUserPredicateEntry(string domain)
      {
        Assert.ArgumentNotNullOrEmpty(domain, nameof(domain));

        this.Domain = domain;
      }

      public string Domain { get; set; }
      public string Pattern { get; set; }
    }
  }
}
