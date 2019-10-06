using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Middleware.Helper;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.BL.Services
{
    public interface IWebSocketHandler
    {
        Task ProcessIncomingRequest(WebSocket webSocket, WebSocketRequestType requestType, BaseDto baseDtoParam = null, string jsonString = null);
        Task CollectFilePeacesFromUsers(List<VirtualFilePieceEntity> filePeacesTheUserDoNotHave);
        Task SendFilePeaceToUser(byte[] fileBytes, Guid user, Guid filePeaceId);
        Task SendDeleteRequestsForFile(VirtualFileEntity file);
    }
}
