using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Sitecore.Diagnostics;

namespace Unicorn.Configuration
{
	public class PredicatePresetHandler : IPredicatePresetHandler
	{
		// ReSharper disable once InconsistentNaming
		protected readonly List<PredicatePreset> _predicatePresets = new List<PredicatePreset>();

		public PredicatePresetHandler(XmlNode configNode)
		{
			Assert.IsNotNull(configNode, "configNode");

			foreach (var element in configNode.ChildNodes.OfType<XmlElement>().Where(x => x.Name.Equals("preset")))
			{
				var id = element.Attributes["id"]?.Value;
				Assert.IsNotNull(id, $"predicatePreset {configNode.BaseURI} includes a preset with no 'id' attribute present");

				var preset = _predicatePresets.Find(pp => pp.Id.Equals(id));
				Assert.IsNull(preset, $"Duplicate Id '{id}' in predicatePreset {configNode.BaseURI}");

				_predicatePresets.Add(new PredicatePreset(element));
			}
		}

		public PredicatePreset GetPresetById(string id)
		{
			return _predicatePresets.Find(pp => pp.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
		}
	}
}
