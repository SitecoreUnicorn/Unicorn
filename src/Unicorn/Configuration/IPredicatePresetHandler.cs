using System.Collections.Generic;

namespace Unicorn.Configuration
{
	public interface IPredicatePresetHandler
	{
		PredicatePreset GetPresetById(string id);
	}
}