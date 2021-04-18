using Eventuous;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features
{
    public record User(string Email, string Token, string Username, string Bio, string Image);

    [ApiController]
    public class UserController : Controller
    {
        private readonly UserService _svc;

        public UserController(UserService svc)
        {
            _svc = svc;
        }
    }

    public class UserService : ApplicationService<Account, AccountState, AccountId>
    {
        public UserService(IAggregateStore store) : base(store)
        {
        }
    }

    public record AccountId(string Value) : AggregateId(Value);

    public record AccountState : AggregateState<AccountState, AccountId>
    {
        public override AccountState When(object @event)
        {
            return this;
        }
    }

    public class Account : Aggregate<AccountState, AccountId>
    {
    }
}