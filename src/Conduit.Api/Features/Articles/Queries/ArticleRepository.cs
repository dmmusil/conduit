using System.Collections.Generic;
using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Projections;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Conduit.Api.Features.Articles.Queries
{
    public class ArticleRepository
    {
        private readonly IConfiguration _config;

        public ArticleRepository(IConfiguration config)
        {
            _config = config;
        }

        public async Task<ArticleDocument?> GetArticleBySlug(string slug)
        {
            const string query = @"select * from Articles where TitleSlug=@slug";
            await using var connection = Connection;
            var article = await connection.QueryFirstOrDefaultAsync<ArticleDocument>(query, new {slug});
            return article;
        }

        public async Task<IEnumerable<string>> GetTags()
        {
            const string query = "select distinct Tag from Tags";
            await using var connection = Connection;
            return await connection.QueryAsync<string>(query);
        }

        public SqlConnection Connection => new SqlConnection(_config.GetConnectionString("ReadModels"));
    }
}