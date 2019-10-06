using System;
using System.Collections.Generic;

namespace ThesisServer.Model.DTO.WebSocketDto.Output
{
    class SendFileDto : OutgoingBaseDto
    {
        public List<Guid> FilePieceIds { get; set; }
    }
}
