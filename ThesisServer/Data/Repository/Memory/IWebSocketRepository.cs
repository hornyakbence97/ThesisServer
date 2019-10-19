using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.Data.Repository.Memory
{
    public interface IWebSocketRepository
    {
        void InsertOrUpdateUser(UserEntity user, WebSocket webSocket);
        bool IsUserAuthenticated(UserEntity userEntity);
        ConcurrentDictionary<string, WebSocket> GetAllActiveUsers();
        void RemoveUser(Guid userId);
    }
}
