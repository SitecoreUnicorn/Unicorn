using System.Net;

namespace Unicorn.ControlPanel.Responses
{
	public class PlainTextResponse : ApiResponse
	{
		public PlainTextResponse(string content) : this(content, HttpStatusCode.OK)
		{
			
		}

		public PlainTextResponse(string content, HttpStatusCode statusCode) : base(statusCode, "text/plain", response => response.Write(content))
		{
			
		}
	}
}
