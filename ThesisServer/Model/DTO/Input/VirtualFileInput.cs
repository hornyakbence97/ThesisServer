using System;

namespace ThesisServer.Model.DTO.Input
{
    public class VirtualFileInput //file piece
    {
        public string Id { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
    }
}
