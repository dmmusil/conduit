using System.Linq;
using System.Threading.Tasks;
using Conduit.Api.Auth;
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
            var profile = await _users.GetUserByUsername(username);
            if (profile == null) return NotFound("Profile not found");

            var caller = (User) HttpContext.Items["User"]!;
            if (caller == null) return Ok(new Profile(profile.Username, profile.Bio, profile.Image, false));
            
            var callerProfile = await _users.GetUserByUuid(caller.Id);
            var following = callerProfile.Following.Contains(profile.Id);
            return Ok(new Profile(profile.Username, profile.Bio, profile.Image, following));
        }

        [HttpPost("follow")]
        [Authorize]
        public async Task<IActionResult> Follow(string username)
        {
            var follower = (User) HttpContext.Items["User"]!;
            
            var account = await _users.GetUserByUsername(username);
            if (account == null) return NotFound($"{username} does not exist.");

            var command = new Commands.FollowUser(new AccountId(follower.Id), account.Id);
            await _svc.Handle(command);

            return Ok(new Profile(account.Username, account.Bio, account.Image, true));
        }
        
        [HttpDelete("follow")]
        [Authorize]
        public async Task<IActionResult> Unfollow(string username)
        {
            var follower = (User) HttpContext.Items["User"]!;
            
            var account = await _users.GetUserByUsername(username);
            if (account == null) return NotFound($"{username} does not exist.");

            var command = new Commands.UnfollowUser(new AccountId(follower.Id), account.Id);
            await _svc.Handle(command);

            return Ok(new Profile(account.Username, account.Bio, account.Image, false));
        }
    }

    public record Profile(string Username, string? Bio, string? Image, bool Following);
}