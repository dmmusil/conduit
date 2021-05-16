using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Events;
using Conduit.Api.Features.Articles.Projections;
using Eventuous.Projections.MongoDB;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Conduit.Api.Features.Articles.Projectors
{
    public class TagsEventHandler : MongoProjection<TagDocument>
    {
        public TagsEventHandler(
            IMongoDatabase database,
            string subscriptionGroup,
            ILoggerFactory loggerFactory) : base(
            database,
            subscriptionGroup,
            loggerFactory)
        {
        }

        protected override ValueTask<Operation<TagDocument>> GetUpdate(
            object evt) =>
            evt switch
            {
                ArticlePublished a => UpdateOperationTask(
                    nameof(TagDocument),
                    update => UpdateCounts(update, a.Tags, 1)),
                ArticleDeleted a => UpdateOperationTask(
                    nameof(TagDocument),
                    update => UpdateCounts(update, a.Tags, -1)),
                _ => NoOp
            };

        private UpdateDefinition<TagDocument> UpdateCounts(
            UpdateDefinitionBuilder<TagDocument> update,
            IEnumerable<string>? tags,
            int delta)
        {
            if (tags == null) return update.Combine();
            var updates =
                tags.Select(tag => update.Inc(d => d.Tags[tag], delta));
            return update.Combine(updates);
        }
    }
}