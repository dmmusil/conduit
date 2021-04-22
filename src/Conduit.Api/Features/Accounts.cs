using System;
using System.Threading.Tasks;
using Conduit.Api.Auth;
using Eventuous;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features
{
    public static class Accounts
    {
        public record UserRegistration(string Email, string Username, string Password);
        public record UserLogin(string Email, string Password);

        public record User(string Email, string Username, string Bio = null, string Image = null, string Token = null);

        public static class Commands
        {
            public record Register(UserRegistration User);

            public record LogIn(UserLogin User);
        }

        public static class Events
        {
            public record UserRegistered(string Email, string Username, string PasswordHash);

            public static void Register()
            {
                TypeMap.AddType<UserRegistered>(nameof(UserRegistered));
            }
        }

        public class UserService : ApplicationService<Account, AccountState, AccountId>
        {
            private readonly IAggregateStore _store;

            public UserService(IAggregateStore store) : base(store)
            {
                _store = store;
                OnNew<Commands.Register>((account, cmd) =>
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(cmd.User.Password);
                    account.Register(cmd.User.Username, cmd.User.Email, hashedPassword);
                });
                OnExisting<Commands.LogIn>(cmd => new AccountId(cmd.User.Email), (_, _) =>
                {
                    // no op
                });
            }

            public async Task<User> Load(string userId)
            {
                var account = await _store.Load<Account>(userId);
                var state = account.State;
                return new User(state.Email, state.Username);
            }
        }

        public record AccountId(string Value) : AggregateId(Value);

        public record AccountState : AggregateState<AccountState, AccountId>
        {
            public override AccountState When(object @event)
            {
                return @event switch
                {
                    Events.UserRegistered(var email, var username, var passwordHash) => this with
                    {
                        Id = new AccountId(email),
                        Email = email,
                        Username = username,
                        PasswordHash = passwordHash
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(@event), "Unknown event")
                };
            }

            public string PasswordHash { get; init; }
            public string Email { get; init; }
            public string Username { get; init; }
            public bool VerifyPassword(string password) => BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }

        public class Account : Aggregate<AccountState, AccountId>
        {
            public void Register(string username, string email, string passwordHash) =>
                Apply(new Events.UserRegistered(email, username, passwordHash));
        }
    }
    
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly Accounts.UserService _svc;
        private readonly JwtIssuer _jwtIssuer;

        public UserController(Accounts.UserService svc, JwtIssuer jwtIssuer)
        {
            _svc = svc;
            _jwtIssuer = jwtIssuer;
        }

        [HttpPost("register")]
        public async Task<Accounts.User> Register([FromBody] Accounts.Commands.Register register)
        {
            var result = await _svc.Handle(register);
            var state = result.State;
            return new Accounts.User(state.Email, state.Username);
        }

        [HttpPost("login")]
        public async Task<Accounts.User> LogIn([FromBody] Accounts.Commands.LogIn login)
        {
            var (state, _) = await _svc.Handle(login);
            var authResult = state.VerifyPassword(login.User.Password);
            return authResult ? new Accounts.User(state.Email, state.Username, Token: _jwtIssuer.GenerateJwtToken(state.Email)) : null;
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetCurrentUser()
        {
            return Ok(HttpContext.Items["User"]);
        }
    }
}