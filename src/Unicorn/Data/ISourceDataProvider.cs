using System.Linq;
using Sitecore.Data;

namespace Unicorn.Data
{
	public interface ISourceDataProvider
	{
		ISourceItem GetItem(string database, ID id);
		void ResetTemplateEngine();
	}
}
