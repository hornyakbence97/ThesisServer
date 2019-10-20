using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Configuration;
using ThesisServer.Infrastructure.Middleware.Helper;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using ThesisServer.Model.DTO.Input;
using ThesisServer.Model.DTO.WebSocketDto.Output;
using DeleteFileDto = ThesisServer.Model.DTO.WebSocketDto.Output.DeleteFileDto;

namespace ThesisServer.BL.Services
{
    public class WebSocketHandler : IWebSocketHandler
    {
        private readonly IWebSocketRepository _webSocketRepository;
        private readonly OnlineUserRepository _onlineUserRepository;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly WebSocketOption _options;
        private readonly Random _random;
        private static bool IsPeriodicalCheckRunning = false;

        public WebSocketHandler(
            IOptions<WebSocketOption> options,
            IWebSocketRepository webSocketRepository,
            OnlineUserRepository onlineUserRepository,
            ILogger<WebSocketHandler> logger,
            IServiceProvider serviceProvider)
        {
            _webSocketRepository = webSocketRepository;
            _onlineUserRepository = onlineUserRepository;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _random = new Random();
            _options = options.Value;

            StartBackgroundWorks();
        }

        private async Task StartBackgroundWorks()
        {
            if (!IsPeriodicalCheckRunning)
            {
                StartPeriodicallyCheck();
            }
        }

        private async Task StartPeriodicallyCheck()
        {
            IsPeriodicalCheckRunning = true;

            await Task.Delay(TimeSpan.FromSeconds(12));

            foreach (var activeUser in _webSocketRepository.GetAllActiveUsers())
            {
                var user = Guid.Parse(activeUser.Key);

                try
                {
                    //sending ping
                    await WriteAsBinaryToWebSocketAsync(
                        bytes: new byte[0],
                        webSocket: activeUser.Value,
                        type: WebSocketMessageType.Binary);
                }
                catch (Exception)
                {
                    _webSocketRepository.RemoveUser(user);
                    _onlineUserRepository.RemoveUser(user);
                }
            }

            StartPeriodicallyCheck();
        }

        private VirtualNetworkDbContext GetDbContext()
        {
            return _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<VirtualNetworkDbContext>();
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

                    _logger.LogDebug($"Incoming websocket request {baseDto.RequestType} from user {baseDto.Token1}");

                    var authenticationDto = JsonConvert.DeserializeObject<AuthenticationDto>(jsonText);
                    await ProcessAuthenticationAsync(authenticationDto, webSocket);
                    break;
                case WebSocketRequestType.RECEIVED_COMFIRMATION:
                    _logger.LogDebug($"Incoming websocket request {baseDto.RequestType} from user {baseDto.Token1}");

                    var receiveConfirmationDto = JsonConvert.DeserializeObject<ReceivedConfirmationDto>(jsonText);
                    await ProcessReceivedConfirmation(receiveConfirmationDto, webSocket);
                    break;
                default:
                    throw new RequestTypeInvalidException(requestType, webSocket);
            }
        }

        public async Task CollectFilePeacesFromUsers(List<VirtualFilePieceEntity> filePeacesToCollect)
        {
            foreach (var filePeaceToCollect in filePeacesToCollect)
            {
                var usersWhoHaveIt = _onlineUserRepository
                    .GetUsersWhoHaveTheFile(filePeaceToCollect)
                    .Select(x => x.ToString())
                    .ToList();

                var webSockets = _webSocketRepository
                    .GetAllActiveUsers()
                    .Where(x => usersWhoHaveIt.Contains(x.Key))
                    .Select(x => x.Value)
                    .ToArray();

                try
                {
                    var selectedWebsocket = webSockets[_random.Next(webSockets.Length)];

                    await SendRequestToSendFilePeace(filePeaceToCollect.FilePieceId, selectedWebsocket);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }

        public async Task SendFilePeaceToUser(byte[] fileBytes, Guid user, Guid filePeaceId)
        {
            var _dbContext = GetDbContext();

            _logger.LogDebug($"Sending file peace {filePeaceId} to user {user}. Filebytes length: {fileBytes.Length}");


            var userToSend = _webSocketRepository
                .GetAllActiveUsers()
                .FirstOrDefault(x => x.Key == user.ToString())
                .Value;

            var userEntity = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == user);

            userEntity.AllocatedSpace += fileBytes.Length;

            await _dbContext.SaveChangesAsync();

            var dtoBytes = AppendArrays(filePeaceId.ToByteArray(), fileBytes);

            //#region Debug

            //var szoveg = string.Join(',', fileBytes);

            //await File.WriteAllTextAsync($"server_{filePeaceId.ToString()}.txt", szoveg, CancellationToken.None);
            
            //#endregion

            await WriteAsBinaryToWebSocketAsync(dtoBytes, userToSend, WebSocketMessageType.Binary);

            //var dto = new SaveFileDto
            //{
            //    RequestType = OutgoingRequestType.SAVE_FILE,
            //    FilePeaces = new List<(byte[] Bytes, Guid Id)> {(fileBytes, filePeaceId)}
            //};

            //await WriteStringToWebSocketAsync(JsonConvert.SerializeObject(dto), userToSend);
        }

        public async Task SendDeleteRequestsForFile(VirtualFileEntity file)
        {

            var users = _onlineUserRepository.UsersInNetworksOnline.FirstOrDefault(x => x.Key == file.NetworkId).Value;

            foreach (var user in users.ToList())
            {
                var websocket = _webSocketRepository.GetAllActiveUsers().FirstOrDefault(x => x.Key == user.ToString()).Value;

                await SendRequestToDeleteFilePeaces(file.FilePieces, websocket);
            }
        }

        private async Task SendRequestToDeleteFilePeaces(List<VirtualFilePieceEntity> filePieces, WebSocket websocket)
        {
            var dto = new DeleteFileDto
            {
                RequestType = OutgoingRequestType.DELETE_FILE,
                FilePiecesToDelete = filePieces.Select(x => x.FilePieceId).ToList()
            };

            _logger.LogDebug($"Sending delete file peace request. FIle peace: {filePieces.FirstOrDefault()?.FilePieceId}");

            await WriteStringToWebSocketAsync(JsonConvert.SerializeObject(dto), websocket);
        }

        private async Task SendRequestToSendFilePeace(Guid filePieceId, WebSocket selectedWebsocket)
        {
            var dto = new SendFileDto
            {
                RequestType = OutgoingRequestType.SEND_FILE,
                FilePieceIds = new List<Guid> {filePieceId}
            };

            _logger.LogDebug($"Sending SEND FILE request of {filePieceId}");

            await WriteStringToWebSocketAsync(JsonConvert.SerializeObject(dto), selectedWebsocket);
        }

        private async Task ProcessReceivedConfirmation(ReceivedConfirmationDto dto, WebSocket webSocket)
        {
            var _dbContext = GetDbContext();

            var userEntity =
                _dbContext
                    .User
                    .FirstOrDefault(x => x.Token1 == dto.Token1 && _webSocketRepository.IsUserAuthenticated(x)) ?? throw new UserNotFoundException(null, webSocket);

            _webSocketRepository.InsertOrUpdateUser(userEntity, webSocket);

            switch (dto.Type)
            {
                case ConfirmationType.SEND_FILE:
                    _logger.LogDebug($"Confirm arrived. {dto.Type} Arrived: {dto.ReceiveId} to user {dto.Token1}");
                    break;
                case ConfirmationType.DELETE_FILE:
                    _logger.LogDebug($"Confirm arrived. {dto.Type} Arrived: {dto.ReceiveId} to user {dto.Token1}");
                    await ProcessDeleteConfirmed(dto);
                    break;
                case ConfirmationType.SAVE_FILE:
                    _logger.LogDebug($"Confirm arrived. {dto.Type} Arrived: {dto.ReceiveId} to user {dto.Token1}");
                    _onlineUserRepository.AddFilePieceToUser(userEntity, dto.ReceiveId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task ProcessDeleteConfirmed(ReceivedConfirmationDto dto)
        {
            var _dbContext = GetDbContext();

            //törölni onlineuserrepo-ból
            //törölni db-ből

            var user = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == dto.Token1) ??
                       throw new OperationFailedException(
                           $"The user {dto.Token1} not found",
                           HttpStatusCode.NotFound,
                           null);

            user.AllocatedSpace -= dto.FilePeaceSize;
            await _dbContext.SaveChangesAsync();

            var filePeace = await _dbContext
                .VirtualFilePiece
                .Include(x => x.File)
                .FirstOrDefaultAsync(x => x.FilePieceId == dto.ReceiveId);

            DeleteFilesRequiredEntity deleteItem;

            try
            {
                deleteItem =
                    await _dbContext.DeleteItems.FirstOrDefaultAsync(x =>
                        x.FileId == filePeace.FileId && x.UserId == dto.Token1);
            }
            catch (Exception)
            {
                _onlineUserRepository.RemoveFilePeaceFromUser(dto.ReceiveId, dto.Token1);
                return;
            }

            _onlineUserRepository.RemoveFilePeaceFromUser(dto.ReceiveId, dto.Token1);

            if (deleteItem != null)
            {
                _dbContext.DeleteItems.Remove(deleteItem);

                await _dbContext.SaveChangesAsync();

                if (!(await _dbContext.DeleteItems.AnyAsync(x => x.FileId == filePeace.FileId)))
                {
                    _dbContext.VirtualFile.Remove(filePeace.File);
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task ProcessAuthenticationAsync(AuthenticationDto dto, WebSocket webSocket)
        {
            var _dbContext = GetDbContext();

            var userEntity =
                await _dbContext
                    .User
                    .FirstOrDefaultAsync(x => x.Token1 == dto.Token1 && x.Token2 == dto.Token2) ?? throw new UserNotFoundException(dto, webSocket);

            _webSocketRepository.InsertOrUpdateUser(userEntity, webSocket);

            await CheckIfThereAreAnyPendingDeletes(webSocket, userEntity);

            await GoIdleAndKeepConnectionOpen(webSocket);
        }

        private async Task CheckIfThereAreAnyPendingDeletes(WebSocket webSocket, UserEntity userEntity)
        {
            var _dbContext = GetDbContext();

            var deleteItems = GetDbContext().DeleteItems.Where(x => x.UserId == userEntity.Token1);

            if (deleteItems.Any())
            {
                foreach (var deleteItem in deleteItems)
                {
                    var filePeaces = await _dbContext.VirtualFilePiece.Where(x => x.FileId == deleteItem.FileId).ToListAsync();
                    await SendRequestToDeleteFilePeaces(filePeaces, webSocket);
                }
            }
        }

        private async Task GoIdleAndKeepConnectionOpen(WebSocket webSocket)
        {
            while (!webSocket.CloseStatus.HasValue && webSocket.State == WebSocketState.Open)
            {
                try
                {
                   var (baseDto, json) = await ReadWebSocketAsAsync<BaseDto>(webSocket);

                    await ProcessIncomingRequest(webSocket, WebSocketRequestType.REQUEST, baseDto, json);
                }
                catch (Exception exception)
                {
                    var user = _webSocketRepository
                        .GetAllActiveUsers()
                        .FirstOrDefault(x => x.Value == webSocket)
                        .Key;

                    var userId = Guid.Parse(user);

                    _logger.LogError($"User {userId} websocket disconnected: {exception}");

                    _onlineUserRepository.RemoveUser(userId);

                    _webSocketRepository.RemoveUser(userId);
                }
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

        //private object _lockObject = new object(); //todo make this generic
        private async Task WriteAsBinaryToWebSocketAsync(byte[] bytes, WebSocket webSocket, WebSocketMessageType type = WebSocketMessageType.Text)
        {
            //await Task.Delay(3000);
            var user = _webSocketRepository.GetAllActiveUsers().FirstOrDefault(x => x.Value == webSocket).Key;


            lock (user) //todo consider this
            {
                webSocket
                        .SendAsync(
                            new ArraySegment<byte>(bytes, 0, bytes.Length),
                            type,
                            true,
                            CancellationToken.None).Wait();
            }
        }

        private async Task<string> ReadStringContentFromWebSocketAsync(WebSocket webSocket)
        {

            var bufferArray = new byte[_options.ReceiveBufferSize];

            var inputResult = await webSocket
                .ReceiveAsync(new ArraySegment<byte>(bufferArray), CancellationToken.None);

            var mainBuffer = new ArraySegment<byte>(bufferArray, 0, inputResult.Count).Array;

            mainBuffer = CutZerosFromTheEnd(mainBuffer, inputResult.Count);

            while (!inputResult.EndOfMessage)
            {
                bufferArray = new byte[_options.ReceiveBufferSize];

                inputResult = await webSocket
                    .ReceiveAsync(new ArraySegment<byte>(bufferArray), CancellationToken.None);

                var temporaryBuffer = new ArraySegment<byte>(bufferArray, 0, inputResult.Count).Array;

                temporaryBuffer = CutZerosFromTheEnd(temporaryBuffer, inputResult.Count);

                mainBuffer = AppendArrays(mainBuffer, temporaryBuffer);
            }

            if (inputResult.MessageType == WebSocketMessageType.Binary)
            {
                await ProcessBinaryAsync(mainBuffer);
                return await ReadStringContentFromWebSocketAsync(webSocket);
            }

            return Encoding.UTF8.GetString(mainBuffer);
        }

        private async Task ProcessBinaryAsync(byte[] mainBuffer)
        {
            if (mainBuffer.Length == 0)
            {
                //todo send back pong
            }
        }

        private static byte[] CutZerosFromTheEnd(byte[] mainBuffer, int inputResultCount)
        {
            var response = new byte[inputResultCount];

            for (int i = 0; i < response.Length; i++)
            {
                response[i] = mainBuffer[i];
            }

            return response;
        }

        public static byte[] AppendArrays(byte[] appendThis, byte[] appendWithThis)
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
