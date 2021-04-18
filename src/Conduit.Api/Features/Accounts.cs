using System;
using System.Threading.Tasks;
using Eventuous;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features
{
    public static class Accounts
    {
        public record UserRegistration(string Email, string Username, string Password);

        public record User(string Email, string Username, string Bio = null, string Image = null, string Token = null);

        public static class Commands
        {
            public record Register(UserRegistration User);
        }

        public static class Events
        {
            public record UserRegistered(string Email, string Username, string PasswordHash);
        }

        [ApiController]
        public class UserController : Controller
        {
            private readonly UserService _svc;

            public UserController(UserService svc)
            {
                _svc = svc;
            }

            public async Task<User> Register(Commands.Register register)
            {
                var result = await _svc.Handle(register);
                var state = result.State;
                return new User(state.Email, state.Username);
            }
        }

        public class UserService : ApplicationService<Account, AccountState, AccountId>
        {
            public UserService(IAggregateStore store) : base(store)
            {
                OnNew<Commands.Register>((account, cmd) =>
                {
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(cmd.User.Password);
                    account.Register(cmd.User.Username, cmd.User.Email, hashedPassword);
                });
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
        }

        public class Account : Aggregate<AccountState, AccountId>
        {
            public void Register(string username, string email, string passwordHash) =>
                Apply(new Events.UserRegistered(email, username, passwordHash));
        }
    }
}