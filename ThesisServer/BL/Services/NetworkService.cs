using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThesisServer.BL.Helper;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;

namespace ThesisServer.BL.Services
{
    public class NetworkService : INetworkService
    {
        private readonly VirtualNetworkDbContext _dbContext;

        public NetworkService(VirtualNetworkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<NetworkEntity> CreateNetwork(string networkName, string passWord)
        {
            var entity = new NetworkEntity
            {
                NetworkId = Guid.NewGuid(),
                NetworkName = networkName,
                NetworkPasswordHash = ComputeSha256Hash(passWord)
            };

            var createdEntity = await _dbContext.Network.AddAsync(entity);

            await _dbContext.SaveDbChangesWithSuccessCheckAsync();

            return createdEntity.Entity;
        }

        public async Task AddUserToNetwork(NetworkEntity networkEntity, UserEntity userEntity, string givenPassword)
        {
            var network =
                await _dbContext.Network.FirstOrDefaultAsync(x => x.NetworkId == networkEntity.NetworkId) 
                ?? throw new OperationFailedException(
                    $"The networkId {networkEntity.NetworkId} not found", HttpStatusCode.NotFound, null);

            var user = 
                await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userEntity.Token1) 
                ?? throw new OperationFailedException($"The user {userEntity.Token1} not found", HttpStatusCode.NotFound, null);

            CheckCredentials(network, givenPassword);

            user.NetworkId = network.NetworkId;
            user.Network = network;

            await _dbContext.SaveDbChangesWithSuccessCheckAsync();
        }

        private void CheckCredentials(NetworkEntity networkEntity, string givenPassword)
        {
            var givePassHash = ComputeSha256Hash(givenPassword);

            if (!networkEntity.NetworkPasswordHash.SequenceEqual(givePassHash))
            {
                throw new OperationFailedException("The given password is incorrect", HttpStatusCode.Forbidden, null);
            }
        }

        private static byte[] ComputeSha256Hash(string rawString)
        {
            using (var sha256Hash = SHA256.Create())
            {
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawString));
            }
        }
    }
}
