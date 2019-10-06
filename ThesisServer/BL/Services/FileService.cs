using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThesisServer.BL.Helper;
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

        public async Task<List<VirtualFilePieceEntity>> GetRelatedFilePeacesForFile(Guid fileId)
        {
            var fileEntity = await _dbContext
                                 .VirtualFile
                                 .Include(x => x.FilePieces)
                                 .Where(x => x.FileId == fileId).FirstOrDefaultAsync()
                             ?? throw new OperationFailedException($"The file {fileId} not found in the db.",
                                 HttpStatusCode.NotFound, null);

            return fileEntity.FilePieces;
        }

        public async Task<VirtualFileEntity> AddFileToDelete(Guid dtoFileId, Guid dtoUserToken1Id)
        {
            var file = await _dbContext
                           .VirtualFile
                           .Include(x => x.FilePieces)
                           .FirstOrDefaultAsync(x => x.FileId == dtoFileId)
                       ?? throw new OperationFailedException($"The file {dtoFileId} not found", HttpStatusCode.NotFound,
                           null);

            var network = await _dbContext
                .Network
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.NetworkId == file.NetworkId);

            if (!network.Users.Any(x => x.Token1 == dtoUserToken1Id))
            {
                throw new OperationFailedException(
                    $"The current user {dtoUserToken1Id} is not a part of the network the given file {dtoFileId} belongs to",
                    HttpStatusCode.Forbidden, null);
            }

            foreach (var user in network.Users)
            {
                await _dbContext.DeleteItems.AddAsync(new DeleteFilesRequiredEntity
                {
                    FileId = file.FileId,
                    UserId = user.Token1
                });
            }

            await _dbContext.SaveDbChangesWithSuccessCheckAsync();

            return file;
        }
    }
}
