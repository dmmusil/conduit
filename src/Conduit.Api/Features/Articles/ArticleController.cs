using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts;
using Conduit.Api.Features.Articles.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Articles
{
    [ApiController]
    [Route("api/articles")]
    [Authorize]
    public class ArticleController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly ArticleService _svc;

        public ArticleController(UserRepository repo, ArticleService svc) =>
            (_repo, _svc) = (repo, svc);

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] Requests.CreateArticle request)
        {
            var user = HttpContext.GetLoggedInUser();
            var (authorId, _, username, _, bio, image, _) =
                await _repo.GetUserByUuid(user.Id);
            var (title, description, body, tags) = request;
            var cmd = new PublishArticle(
                title,
                description,
                body,
                new Author(authorId, username, bio, image, false),
                tags);
            var (a, _) = await _svc.Handle(cmd);
            var response = new ArticleResponse(
                a.Title,
                a.Slug,
                a.Description,
                a.Body,
                a.Author,
                a.CreatedAt,
                a.UpdatedAt,
                false,
                a.FavoriteCount);

            return Ok(response);
        }
    }
}