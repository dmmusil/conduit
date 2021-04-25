using System.Threading.Tasks;
using Conduit.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Accounts
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly Accounts.UserService _svc;
        private readonly JwtIssuer _jwtIssuer;
        private readonly Accounts.UserRepository _users;


        public UsersController(Accounts.UserService svc, JwtIssuer jwtIssuer, Accounts.UserRepository users)
        {
            _svc = svc;
            _jwtIssuer = jwtIssuer;
            _users = users;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Accounts.Commands.Register register)
        {
            if (await _users.EmailExists(register.User.Email)) return Conflict("Email already taken");
            if (await _users.UsernameExists(register.User.Username)) return Conflict("Username already taken");

            var (state, _) = await _svc.Handle(register);
            return Ok(new Accounts.User(state.Id, state.Email, state.Username));
        }

        [HttpPost("login")]
        public async Task<Accounts.User?> LogIn([FromBody] Accounts.Commands.LogIn login)
        {
            var user = await _users.GetUserByEmail(login.User.Email);
            var authResult = user.VerifyPassword(login.User.Password);
            return authResult
                ? new Accounts.User(user.Id, user.Email, user.Username,
                    Token: _jwtIssuer.GenerateJwtToken(user.Id))
                : null;
        }
    }
}