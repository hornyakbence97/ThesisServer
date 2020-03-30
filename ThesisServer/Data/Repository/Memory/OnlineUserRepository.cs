using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.Data.Repository.Memory
{
    public class OnlineUserRepository
    {
        public ConcurrentDictionary<Guid, ConcurrentBag<Guid>> UsersInNetworksOnline { get; set; } //all user id for every user in network
        public ConcurrentDictionary<Guid, ConcurrentBag<Guid>> FilePiecesInUsersOnline { get; set; } //all filepiece the user have

        private object _lockObjectGetUsersWhoHaveTheFile = new object();

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

        public List<Guid> GetUsersWhoHaveTheFile(VirtualFilePieceEntity filePiece)
        {
            var response = new List<Guid>();

            lock (_lockObjectGetUsersWhoHaveTheFile)
            {
                foreach (var user in FilePiecesInUsersOnline)
                {
                    var fp = user.Value.ToList();

                    if (fp.Contains(filePiece.FilePieceId))
                    {
                        response.Add(user.Key);
                    }
                }
            }

            return response;
        }

        public void RemoveFilePeaceFromUser(Guid? filePieceId, Guid userId)
        {
            if (filePieceId == null) return;

            var filePeaces = FilePiecesInUsersOnline.FirstOrDefault(x => x.Key == userId).Value;

            ConcurrentBag<Guid> tenp = new ConcurrentBag<Guid>();

            while (filePeaces != null && filePeaces.Count > 0)
            {
                Guid tmp;
                if (filePeaces.TryTake(out tmp))
                {
                    if (tmp != filePieceId)
                    {
                        tenp.Add(tmp);
                    }
                }
            }

            while (tenp.Count > 0)
            {
                Guid bck;
                if (tenp.TryTake(out bck))
                {
                    filePeaces.Add(bck);
                }
            }
        }

        public void RemoveUser(Guid userId)
        {
            var networkId = UsersInNetworksOnline.FirstOrDefault(x => x.Value.Contains(userId)).Key;

            ConcurrentBag<Guid> temp = new ConcurrentBag<Guid>();
            foreach (var item in UsersInNetworksOnline[networkId])
            {
                if (item.ToString() != userId.ToString())
                {
                    temp.Add(item);
                }
            }

            UsersInNetworksOnline[networkId] = temp;

            if (!temp.Any())
            {
                UsersInNetworksOnline.TryRemove(networkId, out _);
            }

            FilePiecesInUsersOnline.TryRemove(userId, out _);
        }
    }
}
