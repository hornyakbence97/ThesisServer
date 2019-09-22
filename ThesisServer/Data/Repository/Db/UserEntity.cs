using System;

namespace ThesisServer.Data.Repository.Db
{
    public class UserEntity
    {
        public Guid Token1 { get; set; }
        public Guid Token2 { get; set; }
        public string FriendlyName { get; set; }
        public int MaxSpace { get; set; }

        public Guid? NetworkId { get; set; }
        public NetworkEntity Network { get; set; }
    }
}
