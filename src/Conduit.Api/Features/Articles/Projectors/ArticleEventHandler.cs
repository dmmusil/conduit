using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projections;
using Eventuous.Projections.MongoDB;
using Eventuous.Projections.MongoDB.Tools;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Conduit.Api.Features.Articles.Projectors
{
    public class ArticleEventHandler : MongoProjection<ArticleDocument>
    {
        private readonly IMongoDatabase _database;

        public ArticleEventHandler(
            IMongoDatabase database,
            string subscriptionGroup,
            ILoggerFactory loggerFactory) : base(
            database,
            subscriptionGroup,
            loggerFactory)
        {
            _database = database;
        }

        protected override ValueTask<Operation<ArticleDocument>> GetUpdate(
            object evt, long? position) =>
            evt switch
            {
                ArticlePublished e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Set(d => d.TitleSlug, e.TitleSlug)
                        .Set(d => d.Title, e.Title)
                        .Set(d => d.Body, e.Body)
                        .Set(d => d.Description, e.Description)
                        .Set(d => d.AuthorBio, e.AuthorBio)
                        .Set(d => d.AuthorUsername, e.AuthorUsername)
                        .Set(d => d.AuthorImage, e.AuthorImage)
                        .Set(d => d.AuthorId, e.AuthorId)
                        .Set(d => d.PublishDate, e.PublishDate)),
                TitleUpdated e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Set(d => d.Title, e.Title)
                        .Set(d => d.TitleSlug, e.TitleSlug)
                        .Set(d => d.UpdatedDate, e.UpdatedAt)),
                BodyUpdated e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Set(d => d.Body, e.Body)
                        .Set(d => d.UpdatedDate, e.UpdatedAt)),
                DescriptionUpdated e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Set(d => d.Description, e.Description)
                        .Set(d => d.UpdatedDate, e.UpdatedAt)),
                ArticleDeleted e => new ValueTask<Operation<ArticleDocument>>(
                    new OtherOperation<ArticleDocument>(
                        _database
                            .DeleteDocument<ArticleDocument>(e.ArticleId))),
                ArticleFavorited e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Inc(d => d.FavoriteCount, 1)),
                ArticleUnfavorited e => UpdateOperationTask(
                    e.ArticleId,
                    builder => builder.Inc(d => d.FavoriteCount, -1)),
                _ => NoOp
            };
    }
}