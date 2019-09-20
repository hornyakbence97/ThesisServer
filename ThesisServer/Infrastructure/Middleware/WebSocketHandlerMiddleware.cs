using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ThesisServer.BL.Services;
using ThesisServer.Infrastructure.Configuration;
using ThesisServer.Infrastructure.Middleware.Helper;

namespace ThesisServer.Infrastructure.Middleware
{
    public class WebSocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IWebSocketHandler webSocketHandler,
            IOptions<WebSocketOption> options)
        {
            if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/ws")
            {
                var queryString = context.Request.Query[options.Value.RequestType].ToString().AsWebSocketRequestType();

                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                await webSocketHandler.ProcessIncomingRequest(webSocket, queryString);
            }

            else
            {
                await _next(context);
            }
        }
    }
}
