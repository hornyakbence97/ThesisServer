using System;

namespace ThesisServer.Model.DTO.Output
{
    public class UserDto
    {
        public Guid Token1 { get; set; }
        public Guid Token2 { get; set; }
        public string FriendlyName { get; set; }
    }
}
