using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ThesisServer.Data.Repository.Memory;

namespace ThesisServer.Controllers
{
    [Route("Debug")]
    public class DebugController : Controller
    {
        private readonly IWebSocketRepository _webSocketRepository;
        private readonly DebugRepository _debugRepository;

        public DebugController(IWebSocketRepository webSocketRepository, DebugRepository debugRepository)
        {
            _webSocketRepository = webSocketRepository;
            _debugRepository = debugRepository;
        }

        public IActionResult Index()
        {
            return Json((_webSocketRepository.GetAllActiveUsers(), _debugRepository.Errors));
        }
    }
}