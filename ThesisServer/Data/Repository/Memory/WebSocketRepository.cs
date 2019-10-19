using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Pages.Internal.Account;
using Microsoft.Extensions.Options;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Configuration;
using Console = System.Console;

namespace ThesisServer.Data.Repository.Memory
{
    public class WebSocketRepository : IWebSocketRepository
    {
        private readonly IOptions<WebSocketOption> _webSocketOptions;
        private ConcurrentDictionary<string, WebSocket> _activeUsers;

        public WebSocketRepository(IOptions<WebSocketOption> webSocketOptions)
        {
            _webSocketOptions = webSocketOptions;
            _activeUsers = new ConcurrentDictionary<string, WebSocket>();
        }

        public void InsertOrUpdateUser(UserEntity user, WebSocket webSocket)
        {
            _activeUsers.AddOrUpdate(user.Token1.ToString(), webSocket, (guid, socket) => webSocket);

            //var t = new Task(async () =>
            //{
            //    var buffer = new byte[_webSocketOptions.Value.ReceiveBufferSize];
            //    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            //    while (!result.CloseStatus.HasValue)
            //    {
            //        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            //    }
            //});

            //t.Start(TaskScheduler.Default);
        }

        public bool IsUserAuthenticated(UserEntity userEntity)
        {
            return _activeUsers.ContainsKey(userEntity.Token1.ToString());
        }

        public ConcurrentDictionary<string, WebSocket> GetAllActiveUsers()
        {
            return _activeUsers;
        }

        public void RemoveUser(Guid userId)
        {
            _activeUsers.TryRemove(userId.ToString(), out _);
        }
    }
}
