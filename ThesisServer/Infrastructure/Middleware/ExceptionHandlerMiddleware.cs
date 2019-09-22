using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.ExceptionHandle;
using ThesisServer.Infrastructure.Middleware.Helper;

namespace ThesisServer.Infrastructure.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DebugRepository debugRepository)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                debugRepository.Errors.Add((exception.ToString(), DateTime.Now));

                if (!context.Response.HasStarted)
                {
                    if (exception is HandledException handledException)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = (int) handledException.StatusCode();
                        context.Response.ContentType = "application/json";

                        var error = new ErrorClass
                        {
                            StatusCode = context.Response.StatusCode,
                            ErrorMessage = handledException.ErrorMessage()
                        };

                        await context.Response.Body.WriteStringAsStreamAsync(JsonConvert.SerializeObject(error));
                    }

                    else
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "application/json";

                        var error = new ErrorClass
                        {
                            StatusCode = context.Response.StatusCode,
                            ErrorMessage = "Internal server error."
                        };

                        await context.Response.Body.WriteStringAsStreamAsync(JsonConvert.SerializeObject(error));
                    }
                }

                else
                {
                    if (exception is HandledException handledException)
                    {
                        var error = new ErrorClass
                        {
                            StatusCode = (int)handledException.StatusCode(),
                            ErrorMessage = handledException.ErrorMessage()
                        };

                        if (handledException.IsWebSocketException() && handledException.WebSocket() != null)
                        {
                            await handledException.WebSocket()
                                .CloseAsync(
                                    WebSocketCloseStatus.InternalServerError,
                                    statusDescription: $"Status code: {error.StatusCode}\n {error.ErrorMessage}",
                                    cancellationToken: CancellationToken.None);
                        }
                        else
                        {
                            throw;
                        }
                    }

                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
