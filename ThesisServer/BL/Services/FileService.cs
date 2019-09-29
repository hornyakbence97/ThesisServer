using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using ThesisServer.Model.DTO.Input;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.BL.Services
{
    public class FileService : IFileService
    {
        private readonly OnlineUserRepository _onlineUserRepository;
        private readonly VirtualNetworkDbContext _dbContext;
        private readonly IUserService _userService;

        public FileService(
            OnlineUserRepository onlineUserRepository,
            VirtualNetworkDbContext dbContext,
            IUserService userService)
        {
            _onlineUserRepository = onlineUserRepository;
            _dbContext = dbContext;
            _userService = userService;
        }

        public async Task AddFilePiecesToOnlineUserAsync(IEnumerable<VirtualFileInput> filePieces, Guid userId)
        {
            if (filePieces == null) throw new ArgumentNullException(nameof(filePieces));
            var userEntity = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userId)
                             ?? throw new OperationFailedException($"The user {userId} not found",
                                 HttpStatusCode.NotFound, null);

            _onlineUserRepository.AddOnlineUser(userEntity);

            foreach (var filePiece in filePieces)
            {
                _onlineUserRepository.AddFilePieceToUser(
                    user: userEntity,
                    filePieceId: Guid.Parse(filePiece.Id));
            }
        }

        public async Task<List<VirtualFileDto>> FetchAllFilesForUser(Guid userToken1)
        {
            var userEntity = await _userService.GetUserById(userToken1);

            var networkEntity = await _dbContext.Network.FirstOrDefaultAsync(x => x.NetworkId == userEntity.NetworkId)
                ?? throw new OperationFailedException($"The network {userEntity.NetworkId} not found",
                    HttpStatusCode.NotFound, null);

            var files = _dbContext.VirtualFile.Where(x => x.NetworkId == networkEntity.NetworkId);

            var returnList = new List<VirtualFileDto>();

            foreach (var file in files)
            {
                var uploadedByName = string.Empty;
                var modifiedName = string.Empty;

                if (!string.IsNullOrWhiteSpace(file.UploadedBy.ToString()))
                {
                    uploadedByName = (await _userService.GetUserById(file.UploadedBy)).FriendlyName;
                }

                if (!string.IsNullOrWhiteSpace(file.ModifiedBy.ToString()))
                {
                    modifiedName = (await _userService.GetUserById(file.UploadedBy)).FriendlyName;
                }

                returnList.Add(new VirtualFileDto
                {
                    FileId = file.FileId,
                    UploadedBy = uploadedByName,
                    ModifiedBy = modifiedName,
                    FileSize = file.FileSize,
                    LastModified = file.LastModified,
                    Created = file.Created,
                    FileName = file.FileName,
                    MimeType = file.MimeType
                });
            }

            return returnList;
        }
    }
}
