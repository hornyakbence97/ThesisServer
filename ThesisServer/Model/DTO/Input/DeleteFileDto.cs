using System;

namespace ThesisServer.Model.DTO.Input
{
    public class DeleteFileDto
    {
        public Guid FileId { get; set; }
        public Guid UserToken1Id { get; set; }
    }
}
