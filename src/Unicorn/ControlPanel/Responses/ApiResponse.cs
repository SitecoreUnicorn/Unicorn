using System;
using System.Net;
using System.Web;

namespace Unicorn.ControlPanel.Responses
{
	public class ApiResponse : IResponse
	{
		private readonly HttpStatusCode _statusCode;
		private readonly string _contentType;
		private readonly Action<HttpResponseBase> _body;

		public ApiResponse(HttpStatusCode statusCode, string contentType, Action<HttpResponseBase> body)
		{
			_statusCode = statusCode;
			_contentType = contentType;
			_body = body;
		}

		public virtual void Execute(HttpResponseBase response)
		{
			response.StatusCode = (int)_statusCode;

			if (_statusCode != HttpStatusCode.OK) response.TrySkipIisCustomErrors = true;

			response.ContentType = _contentType;

			_body(response);

			response.End();
		}
	}
}
