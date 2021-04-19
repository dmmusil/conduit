using System.Threading.Tasks;
using Conduit.Api.Tests.Fakes;
using Eventuous;
using Xunit;

namespace Conduit.Api.Tests
{
    public class Accounts
    {
        [Fact]
        public async Task Can_be_registered()
        {
            Features.Accounts.Events.Register();
            var aggregateStore = new AggregateStore(
                new InMemoryEventStore(),
                DefaultEventSerializer.Instance);
            var svc = new Features.Accounts.UserService(aggregateStore);

            var user = new Features.Accounts.UserRegistration(
                "jake@jake.jake",
                "jake",
                "jakejake");
            var result = await svc.Handle(new Features.Accounts.Commands.Register(user));

            var actual = result.State;
            Assert.Equal(user.Email, actual.Email);
            Assert.Equal(user.Username, actual.Username);
            Assert.True(BCrypt.Net.BCrypt.Verify(user.Password, actual.PasswordHash));
        }

        [Fact]
        public async Task Can_login()
        {
            // Setup
            Features.Accounts.Events.Register();
            var aggregateStore = new AggregateStore(new InMemoryEventStore(), DefaultEventSerializer.Instance);
            var svc = new Features.Accounts.UserService(aggregateStore);

            // Register a test user
            var user = new Features.Accounts.UserRegistration("jake@jake.jake", "jake", "jakejake");
            await svc.Handle(new Features.Accounts.Commands.Register(user));
            // Log in
            var result = await svc.Handle(
                new Features.Accounts.Commands.LogIn(new Features.Accounts.UserLogin(user.Email, user.Password)));

            Assert.True(result.State.VerifyPassword(user.Password));
        }
    }
}