using System;

namespace ThesisServer.Model.DTO.Input
{
    public class AuthenticationDto : BaseDto
    {
        public Guid Token2 { get; set; }
        public string FriendlyName { get; set; }
    }
}
