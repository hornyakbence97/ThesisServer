using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThesisServer.BL.Services;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;
using ThesisServer.Model.DTO.Input;

namespace ThesisServer.Controllers
{
    [Route("Files")]
    public class FilesController : Controller
    {
        private readonly DebugRepository _debugRepository;
        private readonly IFileService _fileService;

        public FilesController(DebugRepository debugRepository, IFileService fileService)
        {
            _debugRepository = debugRepository;
            _fileService = fileService;
        }

        [Route("UploadIdList/{userId}")]
        public async Task<IActionResult> ReceiveListOfFileIds([FromBody] IEnumerable<VirtualFileInput> input,
            [FromRoute] string userId)
        {
            await _fileService.AddFilePiecesToOnlineUserAsync(input, Guid.Parse(userId));

            return Ok();
        }
    }
}