using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThesisServer.BL.Services;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.Controllers
{
    [Route("Files")]
    public class FilesController : Controller
    {
        private readonly DebugRepository _debugRepository;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;
        private readonly OpenRequestsRepository _openRequestsRepository;
        private readonly IWebSocketHandler _webSocketHandler;
        private readonly IServiceProvider _serviceProvider;

        public FilesController(
            DebugRepository debugRepository,
            IFileService fileService,
            IUserService userService,
            OpenRequestsRepository openRequestsRepository,
            IWebSocketHandler webSocketHandler,
            IServiceProvider serviceProvider)
        {
            _debugRepository = debugRepository;
            _fileService = fileService;
            _userService = userService;
            _openRequestsRepository = openRequestsRepository;
            _webSocketHandler = webSocketHandler;
            _serviceProvider = serviceProvider;
        }

        [Route("UploadIdList/{userId}")]
        public async Task<IActionResult> ReceiveListOfFileIds([FromBody] IEnumerable<VirtualFileInput> input,
            [FromRoute] string userId)
        {
            await _fileService.AddFilePiecesToOnlineUserAsync(input, Guid.Parse(userId));

            return Ok();
        }

        [Route("Fetch")]
        [HttpPost]
        public async Task<IActionResult> FetchFileList([FromBody] UserDto user)
        {
            var files = await _fileService.FetchAllFilesForUser(user.Token1);

            if (files == null)
            {
                return BadRequest();
            }

            return Json(files);
        }

        [Route("SendFilePiece/{filePieceId}")]
        [HttpPost]
        public async Task<IActionResult> SendFilePiece(
            [FromRoute] Guid filePieceId,
            List<IFormFile> filePieces)
        {
            byte[] fileBytes;

            using (var ms = new MemoryStream())
            {
                await filePieces[0].CopyToAsync(ms);

                fileBytes = ms.ToArray();
            }

            var userToSend = await _openRequestsRepository.GetAUserForFilePeaceId(filePieceId);

            if (userToSend != null)
            {
                await _webSocketHandler.SendFilePeaceToUser(fileBytes, userToSend.Token1, filePieceId, null);
            }

            return Ok(fileBytes.Length);
        }

        [Route("OpenFile/{fileId}")]
        [HttpPost]
        public async Task<IActionResult> OpenFileRequest([FromRoute] Guid fileId, [FromBody] Guid userToken1)//todo userToken1 add xamarin request (check if it works)
        {
            var relatedFilePeaces = await _fileService.GetRelatedFilePeacesForFile(fileId);

            var filePeacesTheUserDoNotHave = await _userService.FilterFilePeacesTheUserDoNotHave(relatedFilePeaces, userToken1);

            var responsePrep = relatedFilePeaces.Select(x => (Id: x.FilePieceId, OrderId: x.OrderNumber));

            foreach (var filePieceEntity in filePeacesTheUserDoNotHave)
            {
                _openRequestsRepository.AddItem(filePieceEntity.FilePieceId, userToken1);
            }

            await _webSocketHandler.CollectFilePeacesFromUsers(filePeacesTheUserDoNotHave);

            var response = (MissingIds: filePeacesTheUserDoNotHave.Select(x => (Id: x.FilePieceId, OrderId: x.OrderNumber)), AllIds: relatedFilePeaces.Select(x => (x.FilePieceId, x.OrderNumber)));

            return Json(response);
        }

        [Route("Delete")]
        [HttpPost]
        public async Task<IActionResult> DeleteFile([FromBody] DeleteFileDto dto)
        {
            var file = await _fileService.AddFileToDelete(dto.FileId, dto.UserToken1Id);

            await _webSocketHandler.SendDeleteRequestsForFile(file);

            return Ok();
        }

        [Route("Upload")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 419430400)] // 400 MB
        [RequestSizeLimit(419430400)]
        public async Task<IActionResult> UploadFile(
            IFormFile fileByte)
        {
            var jsonDto = Request.Form["dto"].FirstOrDefault();

            var dto = JsonConvert.DeserializeObject<UploadFileDto>(jsonDto);

            using (var ms = new MemoryStream())
            {
                fileByte.CopyTo(ms);

                dto.FileBytes = ms.ToArray();
            }

            #region Debug

            //if (!Directory.Exists("C:\\tmp"))
            //{
            //    Directory.CreateDirectory("C:\\tmp");
            //}

            //if (Directory.Exists("C:\\tmp\\Incoming"))
            //{
            //    Directory.Delete("C:\\tmp\\Incoming", true);
            //}

            //Directory.CreateDirectory("C:\\tmp\\Incoming");

            //while (!Directory.Exists("C:\\tmp\\Incoming"))
            //{

            //}

            //using (var fs = System.IO.File.Create(Path.Combine("C:\\tmp\\Incoming", fileByte.FileName)))
            //{
            //    var bb = dto.FileBytes;
            //    await fs.WriteAsync(bb, 0, bb.Length);
            //}

            #endregion

            dto.ServiceScope = _serviceProvider.CreateScope();

            ThreadPool.QueueUserWorkItem(
                callBack: new WaitCallback(UploadBackground),
                state: dto);

            GC.Collect();

            return Ok();
        }

        private static async void UploadBackground(object obj)
        {
            if (!(obj is UploadFileDto dto))
            {
                return;
            }

            var fileService = dto.ServiceScope.ServiceProvider.GetRequiredService<IFileService>();

            await fileService.UploadNewFileAsync(dto);

            dto.ServiceScope.Dispose();

            dto = null;
        }

        #region Debug
        [Route("UploadPeaces")]
        [HttpPost]
        public async Task<IActionResult> UploadFilePeacesDebug(
    List<IFormFile> filePieces)
        {
            if (!Directory.Exists("C:\\tmp"))
            {
                Directory.CreateDirectory("C:\\tmp");
            }

            if (Directory.Exists("C:\\tmp\\FilePeaces"))
            {
                Directory.Delete("C:\\tmp\\FilePeaces", true);
            }

            Directory.CreateDirectory("C:\\tmp\\FilePeaces");

            foreach (var formFile in filePieces)
            {
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);

                    using (var fs = System.IO.File.Create(Path.Combine("C:\\tmp\\FilePeaces", formFile.FileName)))
                    {
                        var bb = ms.ToArray();
                        await fs.WriteAsync(bb, 0, bb.Length);
                    }
                }
            }

            return Ok();
        }

        [Route("UploadOpenable")]
        [HttpPost]
        public async Task<IActionResult> UploadOpenableDebug(
            List<IFormFile> filePieces)
        {
            if (!Directory.Exists("C:\\tmp"))
            {
                Directory.CreateDirectory("C:\\tmp");
            }

            if (Directory.Exists("C:\\tmp\\Openable"))
            {
                Directory.Delete("C:\\tmp\\Openable", true);
            }

            Directory.CreateDirectory("C:\\tmp\\Openable");

            foreach (var formFile in filePieces)
            {
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);

                    using (var fs = System.IO.File.Create(Path.Combine("C:\\tmp\\Openable", formFile.FileName + ".mp4")))
                    {
                        var bb = ms.ToArray();
                        await fs.WriteAsync(bb, 0, bb.Length);
                    }
                }
            }

            return Ok();
        }

        [Route("DebugTxt")]
        [HttpPost]
        public async Task<IActionResult> DebugTxt(
           [FromBody] DebugText inn)
        {
            await System.IO.File.WriteAllTextAsync($"client_{inn.Id}.txt", inn.Text, CancellationToken.None);

            return Ok();
        }

        #endregion

    }

    public class DebugText
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
}