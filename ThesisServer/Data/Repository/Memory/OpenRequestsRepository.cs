using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.Data.Repository.Memory
{
    public class OpenRequestsRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        //FilePeaceId, and the userId who want it
        private ConcurrentDictionary<Guid, ConcurrentBag<Guid>> _filePeaceIdTheUserWantsToOpen = new ConcurrentDictionary<Guid, ConcurrentBag<Guid>>();
        private object _lockObject = new object();

        public OpenRequestsRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        private (IServiceScope Scope, VirtualNetworkDbContext DbContext) GetScopeDbContext()
        {
            var scope = _scopeFactory.CreateScope();

            return (scope, scope.ServiceProvider.GetRequiredService<VirtualNetworkDbContext>());
        }

        public void AddItem(Guid filePeaceId, Guid userToken1)
        {
            ConcurrentBag<Guid> listValue;
            var isSuccess = _filePeaceIdTheUserWantsToOpen.TryGetValue(filePeaceId, out listValue);

            if (!isSuccess)
            {
                lock (_lockObject)
                {
                    _filePeaceIdTheUserWantsToOpen.AddOrUpdate(
                        key: filePeaceId,
                        addValue: new ConcurrentBag<Guid> { userToken1 },
                        updateValueFactory: (guid, list) =>
                        {
                            if (list == null)
                            {
                                list = new ConcurrentBag<Guid>();
                            }

                            list.Add(userToken1);

                            return list;
                        });
                }

                return;
            }

            if (!listValue.Contains(userToken1))
            {
                listValue.Add(userToken1);
            }
        }

        public async Task<UserEntity> GetAUserForFilePeaceId(Guid filePeaceId)
        {
            ConcurrentBag<Guid> listValue;
            var isSuccess = _filePeaceIdTheUserWantsToOpen.TryGetValue(filePeaceId, out listValue);

            if (!isSuccess)
            {
                return null;
            }

            Guid response;
            if (listValue.TryTake(out response))
            {
                var providers = GetScopeDbContext();

                var dbContext = providers.DbContext;

                var resp =  await dbContext.User.FirstOrDefaultAsync(x => x.Token1 == response);

                providers.Scope.Dispose();

                return resp;
            }

            return null;
        }
    }
}
