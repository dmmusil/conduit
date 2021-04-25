using System.Threading.Tasks;
using Conduit.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Accounts
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly Accounts.UserService _svc;
        private readonly Accounts.UserRepository _users;

        public UserController(Accounts.UserService svc, Accounts.UserRepository users)
            => (_svc, _users) = (svc, users);

        [HttpGet]
        [Authorize]
        public ActionResult GetCurrentUser()
        {
            return Ok(HttpContext.Items["User"]);
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> Update([FromBody] Accounts.Commands.UpdateUser update)
        {
            var user = (Accounts.User) HttpContext.Items["User"]!;
            if (await _users.EmailExists(update.User.Email, user)) return Conflict("Email already taken");
            if (await _users.UsernameExists(update.User.Username, user)) return Conflict("Username already taken");
            
            var token = user.Token;
            update = update with {StreamId = user.Id};
            var (state, _) = await _svc.Handle(update);
            return Ok(new Accounts.User(state.Id, state.Email, state.Username, state.Bio, state.Image, token));
        }
    }
}