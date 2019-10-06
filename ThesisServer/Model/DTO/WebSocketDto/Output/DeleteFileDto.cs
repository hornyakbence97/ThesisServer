using System;
using System.Collections.Generic;

namespace ThesisServer.Model.DTO.WebSocketDto.Output
{
    public class DeleteFileDto : OutgoingBaseDto
    {
        public List<Guid> FilePiecesToDelete { get; set; }
    }
}
