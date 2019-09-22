using System;

namespace ThesisServer.Model.DTO.Input
{
    public class AddUserToNetworkInput
    {
        public Guid UserId { get; set; }
        public Guid NetworkId { get; set; }
        public string NetworkPassword { get; set; }
    }
}
