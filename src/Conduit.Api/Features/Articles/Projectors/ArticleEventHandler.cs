using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projections;
using Eventuous.Projections.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Conduit.Api.Features.Articles.Projectors
{
    public class ArticleEventHandler : MongoProjection<ArticleDocument>
    {
        public ArticleEventHandler(
            IMongoDatabase database,
            string subscriptionGroup,
            ILoggerFactory loggerFactory) : base(
            database,
            subscriptionGroup,
            loggerFactory)
        {
        }

        protected override ValueTask<Operation<ArticleDocument>> GetUpdate(
            object evt) =>
            evt switch
            {
                ArticlePublished e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Set(d => d.TitleSlug, e.TitleSlug)
                        .Set(d => d.Title, e.Title)
                        .Set(d => d.Body, e.Body)
                        .Set(d => d.Description, e.Description)
                        .Set(d => d.AuthorBio, e.Description)
                        .Set(d => d.AuthorUsername, e.Description)
                        .Set(d => d.AuthorImage, e.Description)
                        .Set(d => d.AuthorId, e.Description)
                        .Set(d => d.PublishDate, e.PublishDate)),
                _ => NoOp
            };
    }
}