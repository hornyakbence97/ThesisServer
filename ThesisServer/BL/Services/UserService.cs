using System;
using System.Net;
using System.Threading.Tasks;
using ThesisServer.BL.Helper;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.BL.Services
{
    public class UserService : IUserService
    {
        private readonly VirtualNetworkDbContext _dbContext;

        public UserService(VirtualNetworkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserEntity> CreateUser(string friendlyName)
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
                Network = null
            };

            var addedUser = await _dbContext.User.AddAsync(user);

            await _dbContext.SaveDbChangesWithSuccessCheckAsync();

            return addedUser.Entity;
        }
    }
}
