using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThesisServer.BL.Helper;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.BL.Services
{
    public class UserService : IUserService
    {
        private readonly VirtualNetworkDbContext _dbContext;
        private readonly OnlineUserRepository _onlineUserRepository;

        public UserService(VirtualNetworkDbContext dbContext, OnlineUserRepository onlineUserRepository)
        {
            _dbContext = dbContext;
            _onlineUserRepository = onlineUserRepository;
        }

        public async Task<UserEntity> CreateUser(string friendlyName, int maxSpace)
        {
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

            return addedUser.Entity;
        }

        public async Task<UserEntity> GetUserById(Guid userToken1)
        {
            return await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userToken1)
                ?? throw new OperationFailedException(
                    message: $"User {userToken1} not found",
                    statusCode: HttpStatusCode.NotFound,
                    webSocket: null);
        }

        public async Task<List<VirtualFilePieceEntity>> FilterFilePeacesTheUserDoNotHave(List<VirtualFilePieceEntity> relatedFilePeaces, Guid userToken1)
        {
            var userEntity = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userToken1)
                             ?? throw new OperationFailedException($"The user {userToken1} not found",
                                 HttpStatusCode.NotFound, null);

            var filePeacesTheUserHave = _onlineUserRepository.FilePiecesInUsersOnline.FirstOrDefault(x => x.Key == userEntity.Token1).Value;

            var response = new List<VirtualFilePieceEntity>();

            foreach (var relatedFilePeace in relatedFilePeaces)
            {
                if (!filePeacesTheUserHave.Any(x => x == relatedFilePeace.FilePieceId))
                {
                    response.Add(relatedFilePeace);
                }
            }

            return response;
        }
    }
}
