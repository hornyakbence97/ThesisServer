using System;
using System.Collections.Generic;

namespace ThesisServer.Model.DTO.WebSocketDto.Output
{
    class SaveFileDto : OutgoingBaseDto
    {
        public List<(byte[] Bytes, Guid Id)> Files { get; set; }
    }
}
