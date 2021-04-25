using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Profiles
{
    [ApiController]
    [Route("api/profiles")]
    public class ProfilesController : ControllerBase
    {
        private readonly Accounts.UserRepository _repository;

        public ProfilesController(Accounts.UserRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetByUsername(string username)
        {
            // todo: implement auth check for determining if this profile is followed
            
            var user = await _repository.GetUserByUsername(username);
            return user != null
                ? Ok(new Profile(user.Username, user.Bio, user.Image, false))
                : NotFound("Profile not found");
        }
    }

    public record Profile(string Username, string? Bio, string? Image, bool Following);
}