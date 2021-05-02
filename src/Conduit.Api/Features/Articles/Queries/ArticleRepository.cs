using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Projections;
using MongoDB.Driver;

namespace Conduit.Api.Features.Articles.Queries
{
    public class ArticleRepository
    {
        private readonly IMongoCollection<ArticleDocument> _database;

        public ArticleRepository(IMongoDatabase database) =>
            _database = database.GetCollection<ArticleDocument>("Article");

        public async Task<ArticleDocument> GetArticleBySlug(string slug)
        {
            var query = await _database.FindAsync(d => d.TitleSlug == slug);
            return await query.SingleOrDefaultAsync();
        }
    }
}