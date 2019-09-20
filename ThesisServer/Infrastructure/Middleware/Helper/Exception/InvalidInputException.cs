using System.Net;
using System.Net.WebSockets;
using ThesisServer.Infrastructure.ExceptionHandle;

namespace ThesisServer.Infrastructure.Middleware.Helper.Exception
{
    public class InvalidInputException : HandledException
    {
        public InvalidInputException(string input, WebSocket webSocket)
        {
            _message = $"This is not a valid request for a websocket. Request body: {input}";
            _isWebSocketException = true;
            _returnStatusCode = HttpStatusCode.BadRequest;
            _webSocket = webSocket;
        }
    }
}
