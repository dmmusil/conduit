using System;
using System.Collections.Generic;
using System.Threading;
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

        [HttpGet, HttpGet("feed"), Authorize]
        public async Task<IActionResult> GetAll()
        {
            var articles =
                await _articleRepository.GetArticlesFromFollowedUsers(
                    HttpContext.GetLoggedInUser().Id);

            return Ok(new FeedEnvelope(articles));
        }

        [HttpPost]
        [Authorize]
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
                Guid.NewGuid().ToString("N"),
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
            var result = await _svc.Handle(cmd, CancellationToken.None);
            var state = result.State;
            return Ok(StateToArticleResponse(state, false));
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var a = await _articleRepository.GetArticleBySlug(slug);
            if (a == null) return NotFound();

            var response = new ArticleResponse(
                a.Title,
                a.TitleSlug,
                a.Description,
                a.Body,
                new Author(
                    a.AuthorId,
                    a.AuthorUsername,
                    a.AuthorBio,
                    a.AuthorImage,
                    false),
                a.PublishDate,
                a.UpdatedDate,
                false,
                a.FavoriteCount,
                a.TagList);
            return Ok(new ArticleEnvelope(response));
        }

        [HttpPut("{slug}")]
        [Authorize]
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

            // set the article ID now that it's known after querying by incoming slug
            update = update with
            {
                Article = update.Article with {ArticleId = article.ArticleId}
            };
            var result = await _svc.Handle(update.Article, CancellationToken.None);
            var state = result.State;
            return Ok(StateToArticleResponse(state, false));
        }

        [HttpDelete("{slug}")]
        [Authorize]
        public async Task<IActionResult> Delete(string slug)
        {
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();

            if (article.AuthorId != HttpContext.GetLoggedInUser().Id)
                return Forbid();

            var cmd = new DeleteArticle(article.ArticleId);
            await _svc.Handle(cmd, CancellationToken.None);
            return NoContent();
        }

        [HttpPost("{slug}/favorite")]
        [Authorize]
        public async Task<IActionResult> FavoriteArticle(string slug)
        {
            var user = HttpContext.GetLoggedInUser();
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();
            var favorite = new FavoriteArticle(article.ArticleId, user.Id);
            var result = await _svc.Handle(favorite, CancellationToken.None);
            var state = result.State;
            return Ok(StateToArticleResponse(state, false));
        }

        [HttpDelete("{slug}/favorite")]
        [Authorize]
        public async Task<IActionResult> UnfavoriteArticle(string slug)
        {
            var user = HttpContext.GetLoggedInUser();
            var article = await _articleRepository.GetArticleBySlug(slug);
            if (article == null) return NotFound();
            var unfavorite = new UnfavoriteArticle(article.ArticleId, user.Id);
            var result = await _svc.Handle(unfavorite, CancellationToken.None);
            var state = result.State;
            return Ok(StateToArticleResponse(state, false));
        }

        private async Task<bool> CheckDuplicateSlug(
            UpdateEnvelope update,
            ArticleDocument article)
        {
            var newSlug = update.Article.Title?.ToSlug();
            var existingSlug = newSlug != null
                ? await _articleRepository.GetArticleBySlug(newSlug)
                : null;
            return existingSlug != null && existingSlug.ArticleId != article.ArticleId;
        }

        private static ArticleEnvelope
            StateToArticleResponse(ArticleState a, bool favorite) =>
            new(new ArticleResponse(
                a.Title,
                a.Slug,
                a.Description,
                a.Body,
                a.Author,
                a.CreatedAt,
                a.UpdatedAt,
                favorite,
                a.FavoriteCount,
                a.Tags ?? Array.Empty<string>()));
    }


    public record UpdateEnvelope(UpdateArticle Article);

    public record FeedEnvelope(IEnumerable<ArticleDocument> Articles);
}