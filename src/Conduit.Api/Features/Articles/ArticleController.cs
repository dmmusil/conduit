using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts.Queries;
using Conduit.Api.Features.Articles.Commands;
using Conduit.Api.Features.Articles.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Articles
{
    [ApiController]
    [Route("api/articles")]
    [Authorize]
    public class ArticleController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly ArticleService _svc;
        private readonly ArticleRepository _articleRepository;

        public ArticleController(
            UserRepository userRepository,
            ArticleService svc,
            ArticleRepository articleRepository) =>
            (_userRepository, _svc, _articleRepository) =
            (userRepository, svc, articleRepository);

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateEnvelope envelope)
        {
            var user = HttpContext.GetLoggedInUser();
            var (authorId, _, username, _, bio, image, _) =
                await _userRepository.GetUserByUuid(user.Id);
            var (title, description, body, tags) = envelope.Article;
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

            return Ok(new ArticleEnvelope(response));
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();

            var response = new ArticleResponse(
                article.Title,
                article.TitleSlug,
                article.Description,
                article.Body,
                new Author(
                    article.AuthorId,
                    article.AuthorUsername,
                    article.AuthorBio,
                    article.AuthorImage,
                    false),
                article.PublishDate,
                article.UpdatedDate,
                false,
                0);
            return Ok(new ArticleEnvelope(response));
        }
    }
}