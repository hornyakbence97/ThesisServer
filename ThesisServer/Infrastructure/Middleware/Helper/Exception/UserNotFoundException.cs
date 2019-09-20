using System.Net;
using System.Net.WebSockets;
using ThesisServer.Infrastructure.ExceptionHandle;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.Infrastructure.Middleware.Helper.Exception
{
    public class UserNotFoundException : HandledException
    {
        public UserNotFoundException(AuthenticationDto user, WebSocket webSocket)
        {
            _message = $"The given user - {user?.FriendlyName} ({user?.Token1}) - not found.";
            _isWebSocketException = true;
            _returnStatusCode = HttpStatusCode.NotFound;
            _webSocket = webSocket;
        }
    }
}
