using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization.Internal;
using ThesisServer.BL.Helper;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.BL.Services
{
    public class UserService : IUserService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OnlineUserRepository _onlineUserRepository;

        private object _lockObjectFilterFilePeacesTheUserDoNotHave = new object();

        public UserService(IServiceProvider serviceProvider, OnlineUserRepository onlineUserRepository)
        {
            _serviceProvider = serviceProvider;
            _onlineUserRepository = onlineUserRepository;
        }

        private (IServiceScope Scope, VirtualNetworkDbContext DbContext) GetScopeDbContext()
        {
            var scope = _serviceProvider.CreateScope();

            return (scope, scope.ServiceProvider.GetRequiredService<VirtualNetworkDbContext>());
        }

        public async Task<UserEntity> CreateUser(string friendlyName, int maxSpace)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                throw new OperationFailedException("You must provide a username", HttpStatusCode.BadRequest, null);
            }

            var user = new UserEntity
            {
                FriendlyName = friendlyName,
                Token1 = Guid.NewGuid(),
                Token2 = Guid.NewGuid(),
                NetworkId = null,
                Network = null,
                MaxSpace = maxSpace
            };

            var addedUser = await _dbContext.User.AddAsync(user);

            await _dbContext.SaveDbChangesWithSuccessCheckAsync();

            providers.Scope.Dispose();

            return addedUser.Entity;
        }

        public async Task<UserEntity> GetUserById(Guid userToken1)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            var response =  await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userToken1)
                ?? throw new OperationFailedException(
                    message: $"User {userToken1} not found",
                    statusCode: HttpStatusCode.NotFound,
                    webSocket: null);

            providers.Scope.Dispose();

            return response;
        }

        public async Task<List<VirtualFilePieceEntity>> FilterFilePeacesTheUserDoNotHave(List<VirtualFilePieceEntity> relatedFilePeaces, Guid userToken1)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            var userEntity = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userToken1)
                             ?? throw new OperationFailedException($"The user {userToken1} not found",
                                 HttpStatusCode.NotFound, null);

            var filePeacesTheUserHave = _onlineUserRepository.FilePiecesInUsersOnline.FirstOrDefault(x => x.Key == userEntity.Token1).Value;

            var response = new List<VirtualFilePieceEntity>();

            foreach (var relatedFilePeace in relatedFilePeaces)
            {
                if (filePeacesTheUserHave != null && !filePeacesTheUserHave.Any(x => x.ToString() == relatedFilePeace.FilePieceId.ToString()))
                {
                    response.Add(relatedFilePeace);
                }
            }

            providers.Scope.Dispose();

            return response;
        }

        public async Task<List<UserEntity>> GetOnlineUsersInNetworkWhoHaveEnoughFreeSpace(
            int fileSettingsFilePeaceMaxSize,
            Guid uploaderUserNetworkId)
        {
            var providers = GetScopeDbContext();

            var _dbContext = providers.DbContext;

            var onlineUsers = _onlineUserRepository.UsersInNetworksOnline[uploaderUserNetworkId];

            var response = new List<UserEntity>();

            foreach (var user in onlineUsers)
            {
                var tmpUser = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == user);

                var freeSpace = (tmpUser.MaxSpace * 1024 * 1024) - tmpUser.AllocatedSpace;

                if (freeSpace >= fileSettingsFilePeaceMaxSize)
                {
                    response.Add(tmpUser);
                }
            }

            providers.Scope.Dispose();

            return response;
        }
    }
}
