using System;
using System.Collections.Concurrent;
using System.Linq;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.Data.Repository.Memory
{
    public class OnlineUserRepository
    {
        public ConcurrentDictionary<Guid, ConcurrentBag<Guid>> UsersInNetworksOnline { get; set; } //all user id for every user in network
        public ConcurrentDictionary<Guid, ConcurrentBag<Guid>> FilePiecesInUsersOnline { get; set; } //all filepiece the user have

        public OnlineUserRepository()
        {
            UsersInNetworksOnline = new ConcurrentDictionary<Guid, ConcurrentBag<Guid>>();
            FilePiecesInUsersOnline = new ConcurrentDictionary<Guid, ConcurrentBag<Guid>>();
        }

        public void AddOnlineUser(UserEntity user)
        {
            ConcurrentBag<Guid> listValue;
            var isSuccess = UsersInNetworksOnline.TryGetValue(user.NetworkId.Value, out listValue);

            if (!isSuccess)
            {
                UsersInNetworksOnline.AddOrUpdate(
                    key: user.NetworkId.Value,
                    addValue: new ConcurrentBag<Guid>{user.Token1},
                    updateValueFactory: (guid, list) =>
                {
                    if (list == null)
                    {
                        list = new ConcurrentBag<Guid>();
                    }

                    list.Add(user.Token1);

                    return list;
                });

                return;
            }

            if (!listValue.Contains(user.Token1))
            {
                listValue.Add(user.Token1);
            }
        }

        public void AddFilePieceToUser(UserEntity user, Guid filePieceId)
        {
            ConcurrentBag<Guid> listValue;
            var isSuccess = FilePiecesInUsersOnline.TryGetValue(user.Token1, out listValue);

            if (!isSuccess)
            {
                FilePiecesInUsersOnline.AddOrUpdate(
                    key: user.Token1,
                    addValue: new ConcurrentBag<Guid> { filePieceId },
                    updateValueFactory: (guid, list) =>
                    {
                        if (list == null)
                        {
                            list = new ConcurrentBag<Guid>();
                        }

                        list.Add(filePieceId);

                        return list;
                    });

                return;
            }

            if (!listValue.Contains(filePieceId))
            {
                listValue.Add(filePieceId);
            }
        }
    }
}
