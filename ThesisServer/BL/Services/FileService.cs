using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThesisServer.BL.Helper;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Infrastructure.Configuration;
using ThesisServer.Infrastructure.Helpers;
using ThesisServer.Infrastructure.Middleware.Helper.Exception;
using ThesisServer.Model.DTO.Input;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.BL.Services
{
    public class FileService : IFileService
    {
        private readonly OnlineUserRepository _onlineUserRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserService _userService;
        private readonly IWebSocketHandler _webSocketHandler;
        private readonly ILogger _logger;
        private readonly FileSettings _fileSettings;
        private readonly Random _random;

        public FileService(
            OnlineUserRepository onlineUserRepository,
            IServiceProvider serviceProvider,
            IUserService userService,
            IOptions<FileSettings> fileSettingsOptions,
            IWebSocketHandler webSocketHandler,
            ILogger<FileService> logger)
        {
            _random = new Random();
            _onlineUserRepository = onlineUserRepository;
            _serviceProvider = serviceProvider;
            _userService = userService;
            _webSocketHandler = webSocketHandler;
            _logger = logger;
            _fileSettings = fileSettingsOptions.Value;
        }

        private VirtualNetworkDbContext GetDbContext()
        {
            return _serviceProvider.CreateScope().ServiceProvider.GetRequiredService<VirtualNetworkDbContext>();
        }

        public async Task AddFilePiecesToOnlineUserAsync(IEnumerable<VirtualFileInput> filePieces, Guid userId)
        {
            var _dbContext = GetDbContext();

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
            var _dbContext = GetDbContext();

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
            var _dbContext = GetDbContext();

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
            var _dbContext = GetDbContext();

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
            //todo disable file so people cannot click on it, and send refresh request
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

        public async Task UploadNewFileAsync(UploadFileDto dto)
        {
            var _dbContext = GetDbContext();

            /*
             * 1. split to file peaces by the default max file peace size
             * 2. Get all the users, who have enough free space to store one peace and online
             * 3. Store all file peace with these users by the following:
             * 4. Calculate redundancy value. e.g.: 10 active phones, 30 pieces, 10% redundancy
             *                 => all phone stores 3 by default, and 3 phone stores 3 more (10%)
             * 5. Store the additional file peaces with other phones (not the same duplicated)
             * 6. add the file to the db
             * 7. add the file peaces to the db
             * 8. save db
             */

            // todo implement upload flow

            var uploaderUser = await _userService.GetUserById(dto.UserToken1);

            var chunks = dto.FileBytes.Chunk(_fileSettings.FilePeaceMaxSize);
            //var redundancy = (double) _fileSettings.RedundancyPercentage / 100; // e.g. 0.1

            var freeUsers = await _userService.GetOnlineUsersInNetworkWhoHaveEnoughFreeSpace(
                _fileSettings.FilePeaceMaxSize,
                uploaderUser.NetworkId.Value);

            if (freeUsers.Count == 0)
            {
                throw new OperationFailedException(
                    message: "There is no enough space to save this item",
                    statusCode: HttpStatusCode.PreconditionFailed,
                    webSocket: null);
            }

            var chunksAndIds = chunks.Select(x => (Bytes: x.Bytes, Id: Guid.NewGuid(), OrderNumber: x.OrderNumber));

            var fileEntity = new VirtualFileEntity
            {
                NetworkId = uploaderUser.NetworkId.Value,
                Created = DateTime.Now,
                FileId = Guid.NewGuid(),
                FileName = dto.FileName,
                FileSize = dto.FileBytes.Length,
                LastModified = DateTime.Now,
                MimeType = dto.MimeType,
                UploadedBy = uploaderUser.Token1
            };

            fileEntity = (await _dbContext.VirtualFile.AddAsync(fileEntity)).Entity;

            var tmpToAddIds = new List<(VirtualFilePieceEntity FIlePieceEntity, byte[] Bytes)>();
            
            foreach (var chunksAndId in chunksAndIds)
            {
                var filePeaceEntity = new VirtualFilePieceEntity
                {
                    FilePieceId = chunksAndId.Id,
                    FileId = fileEntity.FileId,
                    FilePieceSize = chunksAndId.Bytes.Length,
                    OrderNumber = chunksAndId.OrderNumber
                };

                var entity = (await _dbContext.VirtualFilePiece.AddAsync(filePeaceEntity)).Entity;

                tmpToAddIds.Add((entity, chunksAndId.Bytes));
            }

            await _dbContext.SaveChangesAsync();

            chunksAndIds = tmpToAddIds.Select(x => (
                Bytes: x.Bytes,
                Id: x.FIlePieceEntity.FilePieceId,
                OrderNumber: x.FIlePieceEntity.OrderNumber));


            var chunksAndIdsWithRedundancy = AddRedundancy(chunksAndIds.ToArray(), _fileSettings.RedundancyPercentage);

            var relation = new Dictionary<UserEntity, List<(byte[] bytes, Guid Id, int OrderNumber)>>();

            foreach (var freeUser in freeUsers)
            {
                relation.Add(freeUser, new List<(byte[] Bytes, Guid Id, int OrderNumber)>());
            }

            foreach (var fileBytesChunk in chunksAndIdsWithRedundancy)
            {
                var haveTheFewestAssociated = relation.FirstOrDefault(re => re.Value.Count == relation.Min(x => x.Value.Count));

                int tryNumber = 0;

                while (haveTheFewestAssociated.Value.Contains(fileBytesChunk) && tryNumber < 1000)
                {
                    var tmp = relation.ToArray();

                    haveTheFewestAssociated = tmp[_random.Next(tmp.Length)];

                    tryNumber++;
                }

                if (tryNumber < 1000)
                {
                    haveTheFewestAssociated.Value.Add(fileBytesChunk);
                }
            }

            //#region Debug

            //byte[] outp = new byte[fileEntity.FileSize];
            //int i = 0;
            //foreach (var item in chunksAndIds.OrderBy(x => x.OrderNumber).ToList())
            //{
            //    foreach (var itemByte in item.Bytes)
            //    {
            //        outp[i] = itemByte;
            //        i++;
            //    }
            //}

            //bool isEqal = true;
            //for (int j = 0; j < outp.Length; j++)
            //{
            //    if (dto.FileBytes[j] != outp[j])
            //    {
            //        isEqal = false;
            //    }
            //}

            //if (!isEqal)
            //{
            //    throw new ApplicationException("The two array size are not the same");
            //}

            //#endregion

            foreach (var relationItem in relation)
            {
                foreach (var filePeace in relationItem.Value)
                {
                    //todo no need for await, because i want to send immediately to all phones and lock is applied
                    _webSocketHandler.SendFilePeaceToUser(filePeace.bytes, relationItem.Key.Token1, filePeace.Id);
                }
            }

            _logger.LogInformation("SUCCESS. All file has been prepared to sent.");
        }

        private IEnumerable<(byte[] Bytes, Guid Id, int OrderNumber)> AddRedundancy((byte[] Bytes, Guid Id, int OrderNumber)[] chunks, int redundancyPercentage)
        {
            var redundancy = (double) _fileSettings.RedundancyPercentage / 100; // e.g. 0.1

            var additional = (int)Math.Ceiling(chunks.Length * redundancy);

            var selected = new List<(byte[] Bytes, Guid Id, int OrderNumber)>();

            for (int i = 0; i < additional; i++)
            {
                var tmpDuplicate = chunks[_random.Next(chunks.Length)];

                while (selected.Contains(tmpDuplicate))
                {
                    tmpDuplicate = chunks[_random.Next(chunks.Length)];
                }

                selected.Add(tmpDuplicate);
            }

            var response = chunks.Concat(selected.ToArray()).ToArray();

            return response;
        }
    }
}