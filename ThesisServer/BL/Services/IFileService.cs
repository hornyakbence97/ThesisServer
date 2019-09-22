using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.BL.Services
{
    public interface IFileService
    {
        Task AddFilePiecesToOnlineUserAsync(IEnumerable<VirtualFileInput> filepieces, Guid userId);
    }
}
