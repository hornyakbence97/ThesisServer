using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Configuration;
using ThesisServer.Infrastructure.Middleware.Helper;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.BL.Services
{
    public class WebSocketHandler : IWebSocketHandler
    {
        private readonly IWebSocketRepository _webSocketRepository;
        private readonly VirtualNetworkDbContext _dbContext;
        private readonly WebSocketOption _options;

        public WebSocketHandler(
            IOptions<WebSocketOption> options,
            IWebSocketRepository webSocketRepository,
            VirtualNetworkDbContext dbContext)
        {
            _webSocketRepository = webSocketRepository;
            _dbContext = dbContext;
            _options = options.Value;
        }

        public async Task ProcessIncomingRequest(WebSocket webSocket, WebSocketRequestType requestType, BaseDto baseDtoParam = null, string jsonString = null)
        {

            if (requestType != WebSocketRequestType.REQUEST)
            {
                throw new RequestTypeInvalidException(requestType, webSocket);
            }

            BaseDto baseDto;
            string jsonText;

            if (baseDtoParam != null)
            {
                baseDto = baseDtoParam;
                jsonText = jsonString;
            }
            else
            {
                var (baseDtoTemp, jsonTextTemp) = await ReadWebSocketAsAsync<BaseDto>(webSocket);
                baseDto = baseDtoTemp;
                jsonText = jsonTextTemp;
            }

            switch (baseDto.RequestType)
            {
                case WebSocketRequestType.AUTHENTICATION:
                    var authenticationDto = JsonConvert.DeserializeObject<AuthenticationDto>(jsonText);
                    await ProcessAuthenticationAsync(authenticationDto, webSocket);
                    break;
                case WebSocketRequestType.RECEIVED_COMFIRMATION:
                    var receiveConfirmationDto = JsonConvert.DeserializeObject<ReceivedConfirmationDto>(jsonText);
                    await ProcessReceivedConfirmation(receiveConfirmationDto, webSocket);
                    break;
                default:
                    throw new RequestTypeInvalidException(requestType, webSocket);
            }
        }

        private async Task ProcessReceivedConfirmation(ReceivedConfirmationDto dto, WebSocket webSocket)
        {
            await WriteStringToWebSocketAsync("Sziasztok, Bence vagyok, ez pedig itt az ujabb eurocenter hulyeseg itok ide valami {} ilyen is kell haha ", webSocket);

            var userEntity =
                _dbContext
                    .User
                    .FirstOrDefault(x => x.Token1 == dto.Token1 && _webSocketRepository.IsUserAuthenticated(x)) ?? throw new UserNotFoundException(null, webSocket);

            _webSocketRepository.InsertOrUpdateUser(userEntity, webSocket);

            var i = 0;
            while (i < 10)
            {
                await WriteStringToWebSocketAsync(i.ToString(), webSocket);
                await Task.Delay(1000);

                i++;
            }
        }

        private async Task ProcessAuthenticationAsync(AuthenticationDto dto, WebSocket webSocket)
        {
            var userEntity =
                await _dbContext
                    .User
                    .FirstOrDefaultAsync(x => x.Token1 == dto.Token1 && x.Token2 == dto.Token2) ?? throw new UserNotFoundException(dto, webSocket);

            _webSocketRepository.InsertOrUpdateUser(userEntity, webSocket);

            await GoIdleAndKeepConnectionOpen(webSocket);
        }

        private async Task GoIdleAndKeepConnectionOpen(WebSocket webSocket)
        {
            while (!webSocket.CloseStatus.HasValue && webSocket.State == WebSocketState.Open)
            {
                var (baseDto, json) = await ReadWebSocketAsAsync<BaseDto>(webSocket);

                await ProcessIncomingRequest(webSocket, WebSocketRequestType.REQUEST, baseDto, json);
            }
        }

        private async Task<(TResult Model, string Json)> ReadWebSocketAsAsync<TResult>(WebSocket webSocket)
        {
            var jsonText = await ReadStringContentFromWebSocketAsync(webSocket);

            TResult obj;

            try
            {
                obj = JsonConvert.DeserializeObject<TResult>(jsonText);
            }
            catch (Exception e)
            {
                throw new InvalidInputException(jsonText, webSocket);
            }

            return (obj, jsonText);
        }

        private async Task WriteStringToWebSocketAsync(string text, WebSocket webSocket)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            await WriteAsBinaryToWebSocketAsync(bytes, webSocket, WebSocketMessageType.Text);
        }

        private async Task WriteAsBinaryToWebSocketAsync(byte[] bytes, WebSocket webSocket, WebSocketMessageType type = WebSocketMessageType.Text)
        {
            if (bytes.Length <= _options.ReceiveBufferSize)
            {
                await webSocket
                    .SendAsync(
                        new ArraySegment<byte>(bytes, 0, bytes.Length),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);

                return;
            }

            var start = 0;
            var end = _options.ReceiveBufferSize;
            var bufferSize = _options.ReceiveBufferSize;
            var canContinue = true;

            while (canContinue)
            {
                var isEndOfMessage = false;

                if (end >= bytes.Length)
                {
                    var dif = end - bytes.Length;
                    bufferSize -= dif;

                    canContinue = false;

                    isEndOfMessage = true;
                }

                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes, start, bufferSize),
                    type,
                    isEndOfMessage,
                    CancellationToken.None);

                start = end;

                end = end + _options.ReceiveBufferSize;
            }
        }

        private async Task<string> ReadStringContentFromWebSocketAsync(WebSocket webSocket)
        {
            var bufferArray = new byte[_options.ReceiveBufferSize];

            var inputResult = await webSocket
                .ReceiveAsync(new ArraySegment<byte>(bufferArray), CancellationToken.None);

            var mainBuffer = new ArraySegment<byte>(bufferArray, 0, inputResult.Count).Array;

            while (!inputResult.EndOfMessage)
            {
                bufferArray = new byte[_options.ReceiveBufferSize];

                inputResult = await webSocket
                    .ReceiveAsync(new ArraySegment<byte>(bufferArray), CancellationToken.None);

                var temporaryBuffer = new ArraySegment<byte>(bufferArray, 0, inputResult.Count).Array;

                mainBuffer = AppendArrays(mainBuffer, temporaryBuffer);
            }

            return Encoding.UTF8.GetString(mainBuffer);
        }

        private static byte[] AppendArrays(byte[] appendThis, byte[] appendWithThis)
        {
            var tempArray = new byte[appendThis.Length + appendWithThis.Length];

            int i = 0;

            while (i < appendThis.Length)
            {
                tempArray[i] = appendThis[i];
                i++;
            }

            for (int j = 0; j < appendWithThis.Length; j++)
            {
                int s = i + j;
                tempArray[s] = appendWithThis[j];
            }

            return tempArray;
        }
    }
}
