using Microsoft.AspNetCore.Mvc;
using ThesisServer.Data.Repository.Memory;

namespace ThesisServer.Controllers
{
    [Route("Debug")]
    public class DebugController : Controller
    {
        private readonly IWebSocketRepository _webSocketRepository;
        private readonly DebugRepository _debugRepository;
        private readonly OnlineUserRepository _onlineUserRepository;

        public DebugController(
            IWebSocketRepository webSocketRepository,
            DebugRepository debugRepository,
            OnlineUserRepository onlineUserRepository)
        {
            _webSocketRepository = webSocketRepository;
            _debugRepository = debugRepository;
            _onlineUserRepository = onlineUserRepository;
        }

        public IActionResult Index()
        {
            return View((_webSocketRepository.GetAllActiveUsers(), _debugRepository.Errors, _onlineUserRepository.FilePiecesInUsersOnline, _onlineUserRepository.UsersInNetworksOnline));
            //return Json((ActiveUsers: _webSocketRepository.GetAllActiveUsers(), Errors: _debugRepository.Errors));
        }
    }
}