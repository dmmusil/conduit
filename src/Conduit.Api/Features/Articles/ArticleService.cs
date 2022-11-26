using Conduit.Api.Features.Articles.Aggregates;
using Conduit.Api.Features.Articles.Commands;
using Eventuous;
using Eventuous.Subscriptions.Checkpoints;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Features.Articles
{
    public class
        ArticleService : ImmediatelyConsistentApplicationService<Article, ArticleState, ArticleId>
    {
        public ArticleService(
            IAggregateStore store,
            ICheckpointStore checkpointStore,
            ILoggerFactory loggerFactory) :
            base(store, checkpointStore, loggerFactory)
        {
            OnNew<PublishArticle>(
                cmd => new ArticleId(cmd.ArticleId),
                (article, cmd) => article.Publish(
                    new ArticleId(cmd.ArticleId),
                    cmd.Title,
                    cmd.TitleSlug,
                    cmd.Description,
                    cmd.Body,
                    cmd.Author,
                    cmd.Tags));
            OnExisting<UpdateArticle>(
                cmd => new ArticleId(cmd.ArticleId!),
                (article, cmd) => article.Update(
                    cmd.Title,
                    cmd.Description,
                    cmd.Body));
            OnExisting<DeleteArticle>(
                cmd => new ArticleId(cmd.ArticleId),
                (article, cmd) => article.Delete());
            OnExisting<FavoriteArticle>(
                cmd => new ArticleId(cmd.ArticleId),
                (article, cmd) => article.Favorite(cmd.UserId));
            OnExisting<UnfavoriteArticle>(
                cmd => new ArticleId(cmd.ArticleId),
                (article, cmd) => article.Unfavorite(cmd.UserId));
        }
    }
}