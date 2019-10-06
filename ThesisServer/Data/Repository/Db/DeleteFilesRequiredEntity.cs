using System;

namespace ThesisServer.Data.Repository.Db
{
    public class DeleteFilesRequiredEntity
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
    }
}
