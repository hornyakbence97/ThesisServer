using Microsoft.AspNetCore.Builder;

namespace ThesisServer.Infrastructure.Middleware.Helper
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSocketHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketHandlerMiddleware>();
        }

        public static IApplicationBuilder UseHandledExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
