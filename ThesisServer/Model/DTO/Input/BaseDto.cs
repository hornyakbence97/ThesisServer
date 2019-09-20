using System;
using ThesisServer.Infrastructure.Middleware.Helper;

namespace ThesisServer.Model.DTO.Input
{
    public class BaseDto
    {
        public Guid Token1 { get; set; }
        public WebSocketRequestType RequestType { get; set; }
    }
}
