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
            return View((_webSocketRepository.GetAllActiveUsers(), _debugRepository.Errors));
            //return Json((ActiveUsers: _webSocketRepository.GetAllActiveUsers(), Errors: _debugRepository.Errors));
        }
    }
}