using System.Net;
using System.Net.WebSockets;
using ThesisServer.Infrastructure.ExceptionHandle;

namespace ThesisServer.Infrastructure.Middleware.Helper.Exception
{
    public class OperationFailedException : HandledException
    {
        public OperationFailedException(string message, HttpStatusCode statusCode, WebSocket webSocket)
        {
            _message = message;
            _isWebSocketException = true;
            _returnStatusCode = statusCode;
            _webSocket = webSocket;
        }
    }
}
