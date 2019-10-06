using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThesisServer.Data.Repository.Db;

namespace ThesisServer.BL.Services
{
    public interface IUserService
    {
        Task<UserEntity> CreateUser(string friendlyName, int maxSpace);
        Task<UserEntity> GetUserById(Guid fileUploadedBy);
        Task<List<VirtualFilePieceEntity>> FilterFilePeacesTheUserDoNotHave(List<VirtualFilePieceEntity> relatedFilePeaces, Guid userToken1);
    }
}
