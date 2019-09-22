using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ThesisServer.BL.Services;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Model.DTO.Input;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.Controllers
{
    [Route("Network")]
    public class NetworkController : Controller
    {
        private readonly INetworkService _networkService;
        private readonly IMapper _mapper;

        public NetworkController(INetworkService networkService, IMapper mapper)
        {
            _networkService = networkService;
            _mapper = mapper;
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NetworkCreateInput createInput)
        {
            var networkEntity =
                await _networkService.CreateNetwork(createInput.NetworkName, createInput.NetworkPassword);

            return Json(_mapper.Map<NetworkCreateDto>(networkEntity));
        }

        [Route("AddUser")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddUserToNetworkInput addUserInput)
        {
            var user = new UserEntity {Token1 = addUserInput.UserId};
            var network = new NetworkEntity {NetworkId = addUserInput.NetworkId};

            var connected = await _networkService.IsUserConnectedToThisNetwork(user, network);

            if (!connected)
            {
                await _networkService.AddUserToNetwork(network, user, addUserInput.NetworkPassword);
            }

            return Ok();
        }
    }
}