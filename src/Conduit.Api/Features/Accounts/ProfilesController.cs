using System.Threading;
using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts.Aggregates;
using Conduit.Api.Features.Accounts.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Accounts
{
    [ApiController]
    [Route("api/profiles/{username}")]
    public class ProfilesController : ControllerBase
    {
        private readonly UserRepository _users;
        private readonly UserService _svc;

        public ProfilesController(UserRepository users, UserService svc)
        {
            _users = users;
            _svc = svc;
        }

        [HttpGet]
        public async Task<IActionResult> GetByUsername(string username)
        {
            var caller = HttpContext.TryGetLoggedInUser();
            var result = await _users.ProfileWithFollowingStatus(
                username,
                caller?.Id);
            return result != null
                ? Ok(new ProfileEnvelope(result))
                : NotFound("Profile does not exist");
        }

        [HttpPost("follow")]
        [Authorize]
        public async Task<IActionResult> Follow(string username)
        {
            var follower = HttpContext.GetLoggedInUser();

            var account = await _users.GetUserByUsername(username);
            if (account == null) return NotFound($"{username} does not exist.");

            var command = new Commands.FollowUser(
                new AccountId(follower.Id),
                account.Id);
            await _svc.Handle(command, CancellationToken.None);

            return Ok(
                new ProfileEnvelope(
                    new Profile(
                        account.Username,
                        account.Bio,
                        account.Image,
                        true)));
        }

        [HttpDelete("follow")]
        [Authorize]
        public async Task<IActionResult> Unfollow(string username)
        {
            var follower = HttpContext.GetLoggedInUser();

            var account = await _users.GetUserByUsername(username);
            if (account == null) return NotFound($"{username} does not exist.");

            var command = new Commands.UnfollowUser(
                new AccountId(follower.Id),
                account.Id);
            await _svc.Handle(command, CancellationToken.None);

            return Ok(
                new ProfileEnvelope(
                    new Profile(
                        account.Username,
                        account.Bio,
                        account.Image,
                        false)));
        }
    }

    public record Profile(
        string Username,
        string? Bio,
        string? Image,
        bool Following);

    public record ProfileEnvelope(Profile Profile);
}