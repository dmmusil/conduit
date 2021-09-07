using System.Threading;
using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Accounts
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _svc;
        private readonly JwtIssuer _jwtIssuer;
        private readonly UserRepository _users;


        public UsersController(
            UserService svc,
            JwtIssuer jwtIssuer,
            UserRepository users)
        {
            _svc = svc;
            _jwtIssuer = jwtIssuer;
            _users = users;
        }

        [HttpPost]
        public async Task<IActionResult> Register(
            [FromBody] Commands.Register register)
        {
            if (await _users.EmailExists(register.User.Email))
                return Conflict("Email already taken");
            if (await _users.UsernameExists(register.User.Username))
                return Conflict("Username already taken");

            var (state, _) =
                await _svc.Handle(register, CancellationToken.None);
            return Ok(
                new UserEnvelope(
                    new User(
                        state.Id,
                        state.Email,
                        state.Username,
                        Token: _jwtIssuer.GenerateJwtToken(state.Id))));
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] Commands.LogIn login)
        {
            var error = NotFound(
                new
                {
                    Errors = new { InvalidCredentials = "User not found." }
                });
            var user = await _users.GetUserByEmail(login.User.Email);
            if (user == null) return error;
            var authResult = user.VerifyPassword(login.User.Password);
            return authResult
                ? Ok(
                    new UserEnvelope(
                        new User(
                            user.Id,
                            user.Email,
                            user.Username,
                            Token: _jwtIssuer.GenerateJwtToken(user.Id))))
                : error;
        }
    }

    public record UserEnvelope(User User);
}