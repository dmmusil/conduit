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

        public UsersController(UserService svc, JwtIssuer jwtIssuer, UserRepository users)
        {
            _svc = svc;
            _jwtIssuer = jwtIssuer;
            _users = users;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] Commands.Register register)
        {
            if (await _users.EmailExists(register.User.Email))
                return Conflict("Email already taken");
            if (await _users.UsernameExists(register.User.Username))
                return Conflict("Username already taken");

            var result = await _svc.HandleImmediate(register);
            var state = result.State;
            return Ok(
                new UserEnvelope(
                    new User(
                        state.Id,
                        state.Email,
                        state.Username,
                        state.Bio,
                        state.Image,
                        Token: _jwtIssuer.GenerateJwtToken(state.Id)
                    )
                )
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] Commands.LogIn login)
        {
            var error = NotFound(new { Errors = new { InvalidCredentials = "User not found." } });
            var (_, (email, password)) = login;
            var user = await _users.Authenticate(email, password);
            return user != null
                ? Ok(
                    new UserEnvelope(
                        new User(
                            user.Id,
                            user.Email,
                            user.Username,
                            user.Bio,
                            user.Image,
                            Token: _jwtIssuer.GenerateJwtToken(user.Id)
                        )
                    )
                )
                : error;
        }
    }

    public record UserEnvelope(User User);
}
