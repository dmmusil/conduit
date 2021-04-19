using System.Threading.Tasks;
using Xunit;

namespace Conduit.Api.Tests
{
    public class Accounts
    {
        [Fact]
        public async Task Can_be_registered()
        {
            var svc = Fixtures.UserService;

            var user = Fixtures.UserRegistration;
            var result = await svc.Handle(new Features.Accounts.Commands.Register(user));

            var actual = result.State;
            Assert.Equal(user.Email, actual.Email);
            Assert.Equal(user.Username, actual.Username);
            Assert.True(BCrypt.Net.BCrypt.Verify(user.Password, actual.PasswordHash));
        }

        [Fact]
        public async Task Can_login()
        {
            var svc = Fixtures.UserService;
            var user = Fixtures.UserRegistration;
            await svc.Handle(new Features.Accounts.Commands.Register(user));

            var result = await svc.Handle(
                new Features.Accounts.Commands.LogIn(new Features.Accounts.UserLogin(user.Email, user.Password)));

            Assert.True(result.State.VerifyPassword(user.Password));
        }
    }
}