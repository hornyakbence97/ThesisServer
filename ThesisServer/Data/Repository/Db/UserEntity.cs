using System;

namespace ThesisServer.Data.Repository.Db
{
    public class UserEntity
    {
        public Guid Token1 { get; set; }
        public Guid Token2 { get; set; }
        public string FriendlyName { get; set; }
    }
}
