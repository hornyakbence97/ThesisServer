using System.Net;
using System.Net.WebSockets;
using ThesisServer.Infrastructure.ExceptionHandle;

namespace ThesisServer.Infrastructure.Middleware.Helper.Exception
{
    public class RequestTypeInvalidException : HandledException
    {
        public RequestTypeInvalidException(
            WebSocketRequestType requestType,
            WebSocket webSocket,
            HttpStatusCode returnStatusCode = HttpStatusCode.BadRequest)
        {
            _message = $"The request type - {requestType.AsString()} - is not valid";
            _isWebSocketException = true;
            _returnStatusCode = returnStatusCode;
            _webSocket = webSocket;
        }
    }
}
