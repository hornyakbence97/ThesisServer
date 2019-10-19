using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThesisServer.BL.Services;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using Xunit;

namespace Tests.UnitTest
{
    public class UnitTest1 : TestBase
    {
        [Fact]
        public async Task AppendArrays()
        {
            var a = new byte[] { 10, 20, 30, 40, 50};
            var b = new byte[] { 60, 70, 80, 90, 100, 110 };

            var c = new byte[] { 60, 70, 80, 90, 100, 110 };

            var result = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110 };

            var r = WebSocketHandler.AppendArrays(a, b);

            Assert.Equal(result.Length, r.Length);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], r[i]);
            }

            r = WebSocketHandler.AppendArrays(r, c);

            result = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 60, 70, 80, 90, 100, 110 };

            Assert.Equal(result.Length, r.Length);

            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(result[i], r[i]);
            }
        }

        [Fact]
        public async Task AddUserToNetworkWithWrongPasswordShouldFailAsync()
        {
            UserEntity user;
            NetworkEntity createdNetwork;
            var passwordToUse = Guid.NewGuid().ToString();

            using (var scope = _api.Services.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var networkService = scope.ServiceProvider.GetRequiredService<INetworkService>();

                user = await userService.CreateUser("Bence" + DateTime.Now.ToFileTimeUtc(), Int32.MaxValue);

                Assert.NotNull(user);

                createdNetwork =
                    await networkService
                        .CreateNetwork("TestNetwork" + DateTime.Now.ToFileTimeUtc(), passwordToUse);

                Assert.NotNull(createdNetwork);

                await Assert.ThrowsAsync<OperationFailedException>(async () =>
                    await networkService.AddUserToNetwork(createdNetwork, user, Guid.NewGuid().ToString()));
            }

            using (var scope = _api.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VirtualNetworkDbContext>();

                var users = dbContext
                    .Network
                    .Include(x => x.Users)
                    .First(x => x.NetworkId == createdNetwork.NetworkId).Users.Select(x => x.Token1);

                Assert.DoesNotContain(user.Token1, users);
            }
        }

        [Fact]
        public async Task CreateUserAndAddToNetworkAsync()
        {
            UserEntity user;
            NetworkEntity createdNetwork;
            var passwordToUse = Guid.NewGuid().ToString();

            using (var scope = _api.Services.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var networkService = scope.ServiceProvider.GetRequiredService<INetworkService>();

                user = await userService.CreateUser("Bence" + DateTime.Now.ToFileTimeUtc(), Int32.MaxValue);

                Assert.NotNull(user);

                createdNetwork = 
                    await networkService
                        .CreateNetwork("TestNetwork" + DateTime.Now.ToFileTimeUtc(), passwordToUse);

                Assert.NotNull(createdNetwork);

                await networkService.AddUserToNetwork(createdNetwork, user, passwordToUse);
            }

            using (var scope = _api.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VirtualNetworkDbContext>();

                var users = dbContext
                    .Network
                    .Include(x => x.Users)
                    .First(x => x.NetworkId == createdNetwork.NetworkId).Users.Select(x => x.Token1);

                Assert.Contains(user.Token1, users);
            }
        }
    }
}
