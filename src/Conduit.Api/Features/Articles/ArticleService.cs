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
                    cmd.Title.ToSlug(),
                    cmd.Description,
                    cmd.Body,
                    cmd.Author,
                    cmd.Tags));
        }
    }
}