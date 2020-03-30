using System;
using Microsoft.Extensions.DependencyInjection;
using ThesisServer.BL.Services;

namespace ThesisServer.Model.DTO.Input
{
    public class UploadFileDto
    {
        public Guid UserToken1 { get; set; }
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public byte[] FileBytes { get; set; }
        public IServiceScope ServiceScope { get; set; }
    }
}
