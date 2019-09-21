using System;
using System.Collections.Generic;

namespace ThesisServer.Data.Repository.Db
{
    public class NetworkEntity
    {
        public Guid NetworkId { get; set; }
        public string NetworkName { get; set; }
        public byte[] NetworkPasswordHash { get; set; }

        public List<UserEntity> Users { get; set; }
    }
}
