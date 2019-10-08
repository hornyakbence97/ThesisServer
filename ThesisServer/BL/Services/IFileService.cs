using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Model.DTO.Input;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.BL.Services
{
    public interface IFileService
    {
        Task AddFilePiecesToOnlineUserAsync(IEnumerable<VirtualFileInput> filepieces, Guid userId);
        Task<List<VirtualFileDto>> FetchAllFilesForUser(Guid userToken1);
        Task<List<VirtualFilePieceEntity>> GetRelatedFilePeacesForFile(Guid fileId);
        Task<VirtualFileEntity> AddFileToDelete(Guid dtoFileId, Guid dtoUserToken1Id);
        Task UploadNewFileAsync(UploadFileDto dto);
    }
}
