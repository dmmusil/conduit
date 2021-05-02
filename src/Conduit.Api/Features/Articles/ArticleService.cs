using Conduit.Api.Features.Articles.Aggregates;
using Conduit.Api.Features.Articles.Commands;
using Eventuous;

namespace Conduit.Api.Features.Articles
{
    public class
        ArticleService : ApplicationService<Article, ArticleState, ArticleId>
    {
        public ArticleService(IAggregateStore store) : base(store)
        {
            OnNew<PublishArticle>(
                (article, cmd) => article.Publish(
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
        }
    }
}