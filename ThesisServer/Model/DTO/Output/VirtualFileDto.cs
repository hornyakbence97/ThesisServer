using System;

namespace ThesisServer.Model.DTO.Output
{
    public class VirtualFileDto
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public string UploadedBy { get; set; }
        public string MimeType { get; set; }
        public DateTime Created { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsConfirmed { get; set; }
    }
}
