using System.Threading;
using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Accounts
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly UserService _svc;
        private readonly UserRepository _users;

        public UserController(UserService svc, UserRepository users) =>
            (_svc, _users) = (svc, users);

        [HttpGet]
        [Authorize]
        public ActionResult GetCurrentUser()
        {
            return Ok(new UserEnvelope(HttpContext.GetLoggedInUser()));
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> Update(
            [FromBody] Commands.UpdateUser update)
        {
            var user = HttpContext.GetLoggedInUser();
            if (await _users.EmailExists(update.User.Email, user))
                return Conflict("Email already taken");
            if (await _users.UsernameExists(update.User.Username, user))
                return Conflict("Username already taken");

            var token = user.Token;
            update = update with { StreamId = user.Id };
            var result = await _svc.HandleImmediate(update);
            var state = result.State;
            return Ok(
                new UserEnvelope(
                    new User(
                        state.Id,
                        state.Email,
                        state.Username,
                        state.Bio,
                        state.Image,
                        token)));
        }
    }
}