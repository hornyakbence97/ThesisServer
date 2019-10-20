using System;
using System.Collections.Generic;

namespace ThesisServer.Data.Repository.Db
{
    public class VirtualFileEntity
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public Guid UploadedBy { get; set; }
        public string MimeType { get; set; }
        public DateTime Created { get; set; }
        public Guid ModifiedBy { get; set; }
        public bool IsConfirmed { get; set; }

        public List<VirtualFilePieceEntity> FilePieces { get; set; }

        public Guid NetworkId { get; set; }
        public NetworkEntity Network { get; set; }
    }
}
