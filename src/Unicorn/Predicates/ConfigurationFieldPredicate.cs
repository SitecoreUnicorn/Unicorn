using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Diagnostics;

namespace Rainbow.Predicates
{
	/// <summary>
	/// FieldPredicate that reads from an XML config body to define field exclusions, e.g.
	/// <exclude fieldID="{79FA7A4C-378F-425B-B50E-EE2FCC5A8FEF}" note="/sitecore/templates/Sites/Example Site/Subcontent/Test Template/Test/Title" />
	/// </summary>
	public class ConfigurationFieldPredicate : IFieldPredicate
	{
		private readonly HashSet<Guid> _excludedFieldIds = new HashSet<Guid>();
		private readonly HashSet<string> _excludedFieldIdStrings = new HashSet<string>(); 
 
		public ConfigurationFieldPredicate(XmlNode configNode)
		{
			Assert.IsNotNull(configNode, "configNode");
			
			foreach (var element in configNode.ChildNodes.OfType<XmlElement>())
			{
				var attribute = element.Attributes["fieldID"];
				Guid candidate;
				if (attribute == null || !Guid.TryParse(attribute.InnerText, out candidate)) continue;

				_excludedFieldIds.Add(candidate);
				_excludedFieldIdStrings.Add(candidate.ToString());
			}
		}

		public PredicateResult Includes(Guid fieldId)
		{
			return new PredicateResult(!_excludedFieldIds.Contains(fieldId));
		}

		public PredicateResult Includes(string fieldId)
		{
			return new PredicateResult(!_excludedFieldIdStrings.Contains(fieldId));
		}
	}
}
