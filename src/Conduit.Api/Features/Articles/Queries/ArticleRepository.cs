using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Projections;
using MongoDB.Driver;

namespace Conduit.Api.Features.Articles.Queries
{
    public class ArticleRepository
    {
        private readonly IMongoCollection<ArticleDocument> _database;
        private readonly IMongoCollection<TagDocument> _tags;

        public ArticleRepository(IMongoDatabase database) =>
            (_database, _tags) = (
                database.GetCollection<ArticleDocument>("Article"),
                database.GetCollection<TagDocument>("Tag"));

        public async Task<ArticleDocument> GetArticleBySlug(string slug)
        {
            var query = await _database.FindAsync(d => d.TitleSlug == slug);
            return await query.SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<string>> GetTags()
        {
            var query = await _tags.FindAsync(d => d.Id == nameof(TagDocument));
            var result = await query.SingleOrDefaultAsync();
            return result.Tags.OrderByDescending(kv => kv.Key)
                .Where(kv => kv.Value > 0)
                .Select(kv => kv.Key);
        }
    }
}