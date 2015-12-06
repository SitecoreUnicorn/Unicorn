using System.Web;

namespace Unicorn.ControlPanel.Responses
{
	public interface IResponse
	{
		void Execute(HttpResponseBase response);
	}
}
