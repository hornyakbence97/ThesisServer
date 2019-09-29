using System;

namespace ThesisServer.Model.DTO.Input
{
    public class UserDto
    {
        public Guid Token1 { get; set; }
        public Guid Token2 { get; set; }
        public string FriendlyName { get; set; }
        public Guid? NetworkId { get; set; }
        public int MaxSpace { get; set; }
    }
}
