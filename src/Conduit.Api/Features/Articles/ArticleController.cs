using System.Threading.Tasks;
using Conduit.Api.Auth;
using Conduit.Api.Features.Accounts.Queries;
using Conduit.Api.Features.Articles.Aggregates;
using Conduit.Api.Features.Articles.Commands;
using Conduit.Api.Features.Articles.Projections;
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
            var author = await _userRepository.GetUserByUuid(user.Id);
            var (title, description, body, tags) = envelope.Article;

            var existingSlug =
                await _articleRepository.GetArticleBySlug(title.ToSlug());
            if (existingSlug != null)
                return Conflict($"{existingSlug} already exists.");

            var cmd = new PublishArticle(
                title,
                title.ToSlug(),
                description,
                body,
                new Author(
                    user.Id,
                    author.Username,
                    author.Bio,
                    author.Image,
                    false),
                tags);
            var (state, _) = await _svc.Handle(cmd);
            return Ok(StateToArticleResponse(state));
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

        [HttpPut("{slug}")]
        public async Task<IActionResult> UpdateArticle(
            [FromBody] UpdateEnvelope update,
            string slug)
        {
            var author = HttpContext.GetLoggedInUser();
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();
            if (article.AuthorId != author.Id)
                return Forbid("Articles can only be updated by their author.");
            if (await CheckDuplicateSlug(update, article))
                return Conflict(
                    $"{update.Article.Title?.ToSlug()} already exists.");

            update = update with
            {
                Article = update.Article with {ArticleId = article.Id}
            };
            var (state, _) = await _svc.Handle(update.Article);
            return Ok(StateToArticleResponse(state));
        }

        [HttpDelete("{slug}")]
        public async Task<IActionResult> Delete(string slug)
        {
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();

            if (article.AuthorId != HttpContext.GetLoggedInUser().Id)
                return Forbid();

            var cmd = new DeleteArticle(article.Id);
            await _svc.Handle(cmd);
            return NoContent();
        }

        private async Task<bool> CheckDuplicateSlug(
            UpdateEnvelope update,
            ArticleDocument article)
        {
            var newSlug = update.Article.Title?.ToSlug();
            var existingSlug = newSlug != null
                ? await _articleRepository.GetArticleBySlug(newSlug)
                : null;
            return existingSlug != null && existingSlug.Id != article.Id;
        }

        private static ArticleEnvelope StateToArticleResponse(ArticleState a) =>
            new(new ArticleResponse(
                a.Title,
                a.Slug,
                a.Description,
                a.Body,
                a.Author,
                a.CreatedAt,
                a.UpdatedAt,
                false,
                a.FavoriteCount));
    }

    public record UpdateEnvelope(UpdateArticle Article);
}