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
        private readonly IUserService _userService;
        private readonly LockService _lockService;
        private readonly WebSocketOption _options;
        private readonly Random _random;
        private static bool IsPeriodicalCheckRunning = false;

        public WebSocketHandler(
            IOptions<WebSocketOption> options,
            IWebSocketRepository webSocketRepository,
            OnlineUserRepository onlineUserRepository,
            ILogger<WebSocketHandler> logger,
            IServiceProvider serviceProvider,
            IUserService userService,
            LockService lockService)
        {
            _webSocketRepository = webSocketRepository;
            _onlineUserRepository = onlineUserRepository;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _userService = userService;
            _lockService = lockService;
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

            await Task.Delay(TimeSpan.FromSeconds(20));

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

        private (IServiceScope Scope, VirtualNetworkDbContext DbContext) GetScopeDbContext()
        {
            var scope = _serviceProvider.CreateScope();

            return (scope, scope.ServiceProvider.GetRequiredService<VirtualNetworkDbContext>());
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
                    _logger.LogError(exception.Message);
                }
            }
        }

        public async Task SendFilePeaceToUser(byte[] fileBytes, Guid user, Guid filePeaceId, Guid? networkId)
        {
            var isSuccess = false;

            while (!isSuccess)
            {
                isSuccess = true;

                UserEntity userEntity;
                KeyValuePair<string, WebSocket> userKeyValuePair;
                string userStringId = null;
                WebSocket userWebsocketToSend;

                var alreadyTriedUsersList = new List<string>();

                var providers = GetScopeDbContext();

                var _dbContext = providers.DbContext;

                var dtoBytes = AppendArrays(filePeaceId.ToByteArray(), fileBytes);

                try
                {
                    _logger.LogDebug($"Ready to SEND file peace {filePeaceId} to user {user}. Filebytes length: {fileBytes.Length}");

                    userKeyValuePair = _webSocketRepository
                        .GetAllActiveUsers()
                        .FirstOrDefault(x => x.Key == user.ToString());

                    userStringId = userKeyValuePair.Key;

                    userWebsocketToSend = userKeyValuePair.Value;

                    if (!string.IsNullOrWhiteSpace(userStringId))
                    {
                        _logger.LogDebug($"User found to send {userStringId}");

                        lock (_lockService.GetLockObjectForString(userStringId))
                        {
                            _logger.LogDebug($"Lock entered. Locked object: {userStringId}");

                            try
                            {
                                userEntity = _dbContext.User.FirstOrDefault(x => x.Token1 == user);

                                userEntity.AllocatedSpace += fileBytes.Length;

                                _dbContext.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"{e.ToString()}");
                            }
                        }

                        _logger.LogDebug($"Lock leaved. Locked object: {userStringId}");

                        _logger.LogDebug($"Start sending bytes to websocket...");

                        await WriteAsBinaryToWebSocketAsync(dtoBytes, userWebsocketToSend, WebSocketMessageType.Binary);

                        _logger.LogDebug($"SUCCESS. Send OK to user {userStringId}.");
                    }
                    else
                    {
                        _logger.LogError($"User {user} not found in websockets. Send failed.");
                        throw new Exception();
                    }
                
                    #region Debug

                    //var szoveg = string.Join(',', fileBytes);

                    //await File.WriteAllTextAsync($"server_{filePeaceId.ToString()}.txt", szoveg, CancellationToken.None);
            
                    #endregion  
                }
                catch (Exception)
                {
                    _logger.LogError($"Failed to send file peace {filePeaceId} to user {user}");

                    alreadyTriedUsersList.Add(user.ToString());

                    if (!string.IsNullOrWhiteSpace(userStringId))
                    {
                        lock (_lockService.GetLockObjectForString(userStringId))
                        {
                            userEntity = _dbContext.User.FirstOrDefault(x => x.Token1 == user);

                            userEntity.AllocatedSpace -= fileBytes.Length;

                            _dbContext.SaveChanges();
                        }
                    }

                    if (networkId.HasValue)
                    {
                        var availableUsers =
                            await _userService.GetOnlineUsersInNetworkWhoHaveEnoughFreeSpace(
                                fileBytes.Length,
                                networkId.Value);

                        var selected = availableUsers.FirstOrDefault(x =>
                            x.AllocatedSpace == availableUsers
                                .Where(z => !alreadyTriedUsersList.Contains(z.Token1.ToString()))
                                .Min(y => x.AllocatedSpace));

                        userKeyValuePair = _webSocketRepository
                            .GetAllActiveUsers()
                            .FirstOrDefault(x => x.Key == selected.Token1.ToString());

                        userStringId = userKeyValuePair.Key;

                        userWebsocketToSend = userKeyValuePair.Value;

                        try
                        {
                            if (!string.IsNullOrWhiteSpace(userStringId))
                            {
                                lock (_lockService.GetLockObjectForString(userStringId))
                                {
                                    providers = GetScopeDbContext();

                                    _dbContext = providers.DbContext;

                                    selected =_dbContext.User.First(x => x.Token1 == selected.Token1);

                                    selected.AllocatedSpace += fileBytes.Length;
                                    _dbContext.SaveChanges();
                                }

                                await WriteAsBinaryToWebSocketAsync(dtoBytes, userWebsocketToSend, WebSocketMessageType.Binary);
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception)
                        {
                            providers.Scope.Dispose();

                            _logger.LogError($"Failed to send file peace {filePeaceId} to user {selected?.Token1}");

                            alreadyTriedUsersList.Add(selected?.Token1.ToString());

                            if (!string.IsNullOrWhiteSpace(userStringId))
                            {
                                lock (_lockService.GetLockObjectForString(userStringId))
                                {
                                    providers = GetScopeDbContext();
                                    _dbContext = providers.DbContext;

                                    selected = _dbContext.User.First(x => x.Token1 == selected.Token1);

                                    selected.AllocatedSpace -= fileBytes.Length;
                                    _dbContext.SaveChanges();
                                }
                            }

                            availableUsers =
                                await _userService.GetOnlineUsersInNetworkWhoHaveEnoughFreeSpace(
                                    fileBytes.Length,
                                    networkId.Value);

                            selected = availableUsers.FirstOrDefault(x =>
                                x.AllocatedSpace == availableUsers
                                    .Where(z => !alreadyTriedUsersList.Contains(z.Token1.ToString()))
                                    .Max(y => x.AllocatedSpace));

                            userKeyValuePair = _webSocketRepository
                                .GetAllActiveUsers()
                                .FirstOrDefault(x => x.Key == selected.Token1.ToString());

                            userStringId = userKeyValuePair.Key;

                            userWebsocketToSend = userKeyValuePair.Value;

                            try
                            {
                                if (!string.IsNullOrWhiteSpace(userStringId))
                                {
                                    lock (_lockService.GetLockObjectForString(userStringId))
                                    {
                                        providers = GetScopeDbContext();
                                        _dbContext = providers.DbContext;

                                        selected = _dbContext.User.First(x => x.Token1 == selected.Token1);

                                        selected.AllocatedSpace += fileBytes.Length;
                                        _dbContext.SaveChanges();
                                    }

                                    await WriteAsBinaryToWebSocketAsync(dtoBytes, userWebsocketToSend, WebSocketMessageType.Binary);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }

                            catch (Exception)
                            {
                                if (!string.IsNullOrWhiteSpace(userStringId))
                                {
                                    lock (_lockService.GetLockObjectForString(userStringId))
                                    {
                                        providers = GetScopeDbContext();
                                        _dbContext = providers.DbContext;

                                        selected = _dbContext.User.First(x => x.Token1 == selected.Token1);

                                        selected.AllocatedSpace -= fileBytes.Length;
                                        _dbContext.SaveChanges();

                                    }
                                }

                                isSuccess = false;
                                _logger.LogError($"Failed to send file peace {filePeaceId} to any user");
                            }
                        
                        }
                    }
                }

                providers.Scope.Dispose();

               await Task.Delay(1500);
            }
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
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

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
                    await SaveFileConfirmationProcess(dto, userEntity);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            providers.Scope.Dispose();
        }

        private async Task SaveFileConfirmationProcess(ReceivedConfirmationDto dto, UserEntity userEntity)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            _logger.LogDebug($"Confirm arrived. {dto.Type} Arrived: {dto.ReceiveId} to user {dto.Token1}");
            _onlineUserRepository.AddFilePieceToUser(userEntity, dto.ReceiveId);

            var filePeace = await _dbContext
                .VirtualFilePiece
                .Include(x => x.File)
                .FirstOrDefaultAsync(x => x.FilePieceId == dto.ReceiveId);

            filePeace.IsConfirmed = true;

            await _dbContext.SaveChangesAsync();

            var connectedFilePeEntities = _dbContext
                .VirtualFilePiece
                .Where(x => x.FileId == filePeace.FileId);

            if (!connectedFilePeEntities.Any(x => x.IsConfirmed == false))
            {
                filePeace.File.IsConfirmed = true;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"The file {filePeace.FileId} ONLINE");
            }

            providers.Scope.Dispose();
        }

        private async Task ProcessDeleteConfirmed(ReceivedConfirmationDto dto)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

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
                providers.Scope.Dispose();
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

            providers.Scope.Dispose();
        }

        private async Task ProcessAuthenticationAsync(AuthenticationDto dto, WebSocket webSocket)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            var userEntity =
                await _dbContext
                    .User
                    .FirstOrDefaultAsync(x => x.Token1 == dto.Token1 && x.Token2 == dto.Token2) ?? throw new UserNotFoundException(dto, webSocket);

            _webSocketRepository.InsertOrUpdateUser(userEntity, webSocket);

            providers.Scope.Dispose();

            await CheckIfThereAreAnyPendingDeletes(webSocket, userEntity);

            await GoIdleAndKeepConnectionOpen(webSocket);
        }

        private async Task CheckIfThereAreAnyPendingDeletes(WebSocket webSocket, UserEntity userEntity)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            var deleteItems = _dbContext.DeleteItems.Where(x => x.UserId == userEntity.Token1);

            if (deleteItems.Any())
            {
                foreach (var deleteItem in deleteItems)
                {
                    var filePeaces = await _dbContext.VirtualFilePiece.Where(x => x.FileId == deleteItem.FileId).ToListAsync();
                    await SendRequestToDeleteFilePeaces(filePeaces, webSocket);
                }
            }

            providers.Scope.Dispose();
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

                    if (!string.IsNullOrWhiteSpace(user))
                    {
                        var userId = Guid.Parse(user);

                        _logger.LogError($"User {userId} websocket disconnected: {exception}");

                        _onlineUserRepository.RemoveUser(userId);

                        _webSocketRepository.RemoveUser(userId);
                    }

                    _logger.LogError($"User websocket disconnected: {exception}");
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
            //var user = _webSocketRepository.GetAllActiveUsers().FirstOrDefault(x => x.Value == webSocket).Key;

            byte[] bytes = Encoding.UTF8.GetBytes(text);

            //lock (user)
            //{
                await WriteAsBinaryToWebSocketAsync(bytes, webSocket, WebSocketMessageType.Text);
            ////}
        }

        //private object _lockObject = new object(); //todo make this generic
        private async Task WriteAsBinaryToWebSocketAsync(byte[] bytes, WebSocket webSocket, WebSocketMessageType type = WebSocketMessageType.Text)
        {
            //await Task.Delay(3000);
            string user;

            try
            {
                user = _webSocketRepository
                    .GetAllActiveUsers()
                    .FirstOrDefault(x => x.Value == webSocket)
                    .Key;

                if (string.IsNullOrWhiteSpace(user))
                {
                    throw new OperationFailedException("User offline", HttpStatusCode.InternalServerError, webSocket);
                }
            }
            catch(Exception)
            {
                _logger.LogError("Not able to write websocket to user, because not found");
                throw new OperationFailedException("User offline", HttpStatusCode.InternalServerError, webSocket);
            }

            bool isFailed = false;
            lock (_lockService.GetLockObjectForString(user))
            {
                _logger.LogInformation($"ENTERED lock while sending websocket. Object: {user}");

                try
                {
                    webSocket
                        .SendAsync(
                            new ArraySegment<byte>(bytes, 0, bytes.Length),
                            type,
                            true,
                            new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token)
                        .Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception)
                {
                    _logger.LogError($"An error happened during writing to websocket...");
                    isFailed = true;
                }
            }

            _logger.LogInformation($"LEFT lock while sending websocket. Object: {user}");

            if (isFailed)
            {
                _logger.LogError("Write to websocket was unsuccessful");
                throw new OperationFailedException("User offline", HttpStatusCode.InternalServerError, webSocket);
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
