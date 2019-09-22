using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.BL.Services
{
    public class FileService : IFileService
    {
        private readonly OnlineUserRepository _onlineUserRepository;
        private readonly VirtualNetworkDbContext _dbContext;

        public FileService(OnlineUserRepository onlineUserRepository, VirtualNetworkDbContext dbContext)
        {
            _onlineUserRepository = onlineUserRepository;
            _dbContext = dbContext;
        }

        public async Task AddFilePiecesToOnlineUserAsync(IEnumerable<VirtualFileInput> filePieces, Guid userId)
        {
            if (filePieces == null) throw new ArgumentNullException(nameof(filePieces));
            var userEntity = await _dbContext.User.FirstOrDefaultAsync(x => x.Token1 == userId)
                             ?? throw new OperationFailedException($"The user {userId} not found", HttpStatusCode.NotFound, null);
            ;

            _onlineUserRepository.AddOnlineUser(userEntity);

            foreach (var filePiece in filePieces)
            {
                _onlineUserRepository.AddFilePieceToUser(
                    user: userEntity,
                    filePieceId: Guid.Parse(filePiece.Id));
            }
        }
    }
}
