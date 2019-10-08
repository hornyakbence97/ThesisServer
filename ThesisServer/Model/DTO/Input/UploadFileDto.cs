using System;

namespace ThesisServer.Model.DTO.Input
{
    public class UploadFileDto
    {
        public Guid UserToken1 { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public byte[] FileBytes { get; set; }
    }
}
