using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public FilesController(
            DebugRepository debugRepository,
            IFileService fileService,
            IUserService userService,
            OpenRequestsRepository openRequestsRepository,
            IWebSocketHandler webSocketHandler)
        {
            _debugRepository = debugRepository;
            _fileService = fileService;
            _userService = userService;
            _openRequestsRepository = openRequestsRepository;
            _webSocketHandler = webSocketHandler;
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
                await _webSocketHandler.SendFilePeaceToUser(fileBytes, userToSend.Token1, filePieceId);
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
        public async Task<IActionResult> UploadFile(
            IFormFile fileByte)
        {
            var jsonDto = Request.Form["dto"].FirstOrDefault();

            var dto = JsonConvert.DeserializeObject<UploadFileDto>(jsonDto);

            using (var ms = new MemoryStream())
            {
                await fileByte.CopyToAsync(ms);

                dto.FileBytes = ms.ToArray();
            }

            await _fileService.UploadNewFileAsync(dto);

            return Ok();
        }
    }
}