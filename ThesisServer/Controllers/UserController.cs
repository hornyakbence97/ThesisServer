using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ThesisServer.BL.Services;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.Controllers
{
    [Route("User")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UserController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [Route("CreateUser/{friendlyName}/{maxSpace}")]
        public async Task<IActionResult> CreateUser([FromRoute] string friendlyName, [FromRoute] int maxSpace)
        {
            var userCreated = await _userService.CreateUser(friendlyName, maxSpace);

            return Json(_mapper.Map<UserDto>(userCreated));
        }
    }
}