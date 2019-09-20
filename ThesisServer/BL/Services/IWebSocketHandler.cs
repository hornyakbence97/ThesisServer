using System.Net.WebSockets;
using System.Threading.Tasks;
using ThesisServer.Infrastructure.Middleware.Helper;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.BL.Services
{
    public interface IWebSocketHandler
    {
        Task ProcessIncomingRequest(WebSocket webSocket, WebSocketRequestType requestType, BaseDto baseDtoParam = null, string jsonString = null);
    }
}
