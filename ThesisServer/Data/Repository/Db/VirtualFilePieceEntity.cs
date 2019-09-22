using System;

namespace ThesisServer.Data.Repository.Db
{
    public class VirtualFilePieceEntity
    {
        public Guid FilePieceId { get; set; }
        public long FilePieceSize { get; set; }

        public Guid FileId { get; set; }
        public VirtualFileEntity File { get; set; }
    }
}
