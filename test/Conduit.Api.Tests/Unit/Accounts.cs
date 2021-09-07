using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Conduit.Api.Tests.Unit
{
    public class Accounts
    {
        [Fact]
        public async Task Can_be_registered()
        {
            var svc = Fixtures.UserService;

            var user = Fixtures.UserRegistration;
            var result = await svc.Handle(
                new Features.Accounts.Commands.Register(user),
                CancellationToken.None);
            ;

            var actual = result.State;
            Assert.Equal(user.Email, actual.Email);
            Assert.Equal(user.Username, actual.Username);
            Assert.True(
                BCrypt.Net.BCrypt.Verify(user.Password, actual.PasswordHash));
        }
    }
}