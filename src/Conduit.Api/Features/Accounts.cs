using System;
using System.Threading.Tasks;
using Conduit.Api.Auth;
using Eventuous;
using Eventuous.Projections.MongoDB.Tools;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Conduit.Api.Features
{
    public static class Accounts
    {
        public record UserRegistration(string Email, string Username, string Password);

        public record UserLogin(string Email, string Password);

        public record UserUpdate(string? Email, string? Username, string? Password, string? Bio,
            string? Image);

        public record User(string Id, string Email, string Username, string? Bio = null, string? Image = null,
            string? Token = null);


        public static class Commands
        {
            public record Register(UserRegistration User);

            public record LogIn(string? StreamId, UserLogin User);

            public record UpdateUser(string? StreamId, UserUpdate User);
        }

        public static class Events
        {
            public record UserRegistered(string StreamId, string Email, string Username, string PasswordHash);

            public record UsernameUpdated(string StreamId, string Username);

            public record PasswordUpdated(string StreamId, string PasswordHash);

            public record BioUpdated(string StreamId, string Bio);

            public record ImageUpdated(string StreamId, string Image);

            public record EmailUpdated(string StreamId, string Email);

            public static void Register()
            {
                TypeMap.AddType<UserRegistered>(nameof(UserRegistered));
                TypeMap.AddType<UsernameUpdated>(nameof(UsernameUpdated));
                TypeMap.AddType<PasswordUpdated>(nameof(PasswordUpdated));
                TypeMap.AddType<BioUpdated>(nameof(BioUpdated));
                TypeMap.AddType<ImageUpdated>(nameof(ImageUpdated));
                TypeMap.AddType<EmailUpdated>(nameof(EmailUpdated));
            }
        }

        public static class Projections
        {
            public record UserDocument(
                    string StreamId,
                    string Email,
                    string Username,
                    string PasswordHash,
                    string? Bio,
                    string? Image)
                : ProjectedDocument(StreamId)
            {
                public bool VerifyPassword(string password) => BCrypt.Net.BCrypt.Verify(password, PasswordHash);
            }
        }

        public class UserService : ApplicationService<Account, AccountState, AccountId>
        {
            private readonly IAggregateStore _store;

            public UserService(IAggregateStore store) : base(store)
            {
                _store = store;
                OnNew<Commands.Register>(
                    (account, cmd) =>
                    {
                        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(cmd.User.Password);
                        account.Register(cmd.User.Username, cmd.User.Email, hashedPassword);
                    });
                OnExisting<Commands.UpdateUser>(
                    cmd => new AccountId(cmd.StreamId!),
                    (account, cmd) =>
                    {
                        var (email, username, password, bio, image) = cmd.User;
                        account.Update(email, username, password, bio, image);
                    });
            }

            public async Task<User> Load(string userId)
            {
                var account = await _store.Load<Account>(userId);
                var state = account.State;
                return new User(state.Id, state.Email, state.Username);
            }
        }

        public record AccountId(string Value) : AggregateId(Value);

        public record AccountState : AggregateState<AccountState, AccountId>
        {
            public override AccountState When(object @event)
            {
                return @event switch
                {
                    Events.UserRegistered(var streamId, var email, var username, var passwordHash) => this with
                    {
                        Id = new AccountId(streamId),
                        Email = email,
                        Username = username,
                        PasswordHash = passwordHash
                    },
                    Events.EmailUpdated(_, var email) => this with {Email = email},
                    Events.UsernameUpdated(_, var username) => this with {Username = username},
                    Events.BioUpdated(_, var bio) => this with {Bio = bio},
                    Events.PasswordUpdated(_, var passwordHash) => this with {PasswordHash = passwordHash},
                    Events.ImageUpdated(_, var image) => this with {Image = image},
                    _ => throw new ArgumentOutOfRangeException(nameof(@event), "Unknown event")
                };
            }

            public string PasswordHash { get; private init; } = null!;
            public string Email { get; private init; } = null!;
            public string Username { get; private init; } = null!;
            public bool AlreadyRegistered => Id != null;
            public string? Bio { get; private init; }
            public string? Image { get; private init; }
        }

        public class Account : Aggregate<AccountState, AccountId>
        {
            public void Register(string username, string email, string passwordHash)
            {
                if (State.AlreadyRegistered) return;
                Apply(new Events.UserRegistered(Guid.NewGuid().ToString("N"), email, username, passwordHash));
            }

            public void Update(string? email, string? username, string? password, string? bio, string? image)
            {
                EnsureExists();
                if (email != null) Apply(new Events.EmailUpdated(State.Id, email));
                if (username != null) Apply(new Events.UsernameUpdated(State.Id, username));
                if (password != null)
                    Apply(new Events.PasswordUpdated(State.Id, BCrypt.Net.BCrypt.HashPassword(password)));
                if (bio != null) Apply(new Events.BioUpdated(State.Id, bio));
                if (image != null) Apply(new Events.ImageUpdated(State.Id, image));
            }
        }

        public class UserRepository
        {
            private readonly IMongoCollection<Projections.UserDocument> _database;

            public UserRepository(IMongoDatabase database) =>
                _database = database.GetCollection<Projections.UserDocument>("User");

            public async Task<Projections.UserDocument> GetUserByEmail(string email)
            {
                var query = await _database.FindAsync(d => d.Email == email);
                return await query.SingleOrDefaultAsync();
            }

            public async Task<Projections.UserDocument> GetUserByUsername(string username)
            {
                var query = await _database.FindAsync(d => d.Username == username);
                return await query.SingleOrDefaultAsync();
            }
            
            public async Task<bool> UsernameExists(string? username, User? user = null)
            {
                if (username == null) return false;
                var userByUsername = await GetUserByUsername(username);
                return userByUsername != null && userByUsername.Id != user?.Id;
            }

            public async Task<bool> EmailExists(string? email, User? user = null)
            {
                if (email == null) return false;
                var userWithEmail = await GetUserByEmail(email);
                return userWithEmail != null && userWithEmail.Id != user?.Id;
            }

        }
    }

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