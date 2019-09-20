using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.Infrastructure.Middleware.Helper
{
    public static class Extensions
    {
        public static WebSocketRequestType AsWebSocketRequestType(this string requestType)
        {
            if (string.IsNullOrWhiteSpace(requestType))
            {
                throw new RequestTypeNotProvidedException();
            }

            Enum.TryParse(requestType, true, out WebSocketRequestType response);

            return response;
        }

        public static string AsString(this WebSocketRequestType type)
        {
            return type.ToString();
        }

        public static async Task WriteStringAsStreamAsync(this Stream httpResponse, string text)
        {
            var encoding = Encoding.UTF8.GetBytes(text);
            await httpResponse.WriteAsync(encoding, 0, encoding.Length);
        }
    }
}
