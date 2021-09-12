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
            const string query = @"
select * from Articles where TitleSlug=@slug
";
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

        private SqlConnection Connection => new SqlConnection(_config.GetConnectionString("ReadModels"));

        public async Task<IEnumerable<ArticleDocument>> GetArticlesFromFollowedUsers(string userId)
        {
            const string query = @"
            select 
                   a.ArticleId
                 , a.Title
                 , a.TitleSlug
                 , a.Description
                 , a.Body
                 , a.AuthorId
                 , a.AuthorUsername
                 , a.AuthorBio
                 , a.AuthorImage
                 , a.PublishDate
                 , a.UpdatedDate
                 , a.FavoriteCount 
            from Articles as a
            join Followers as f on f.FollowedUserId = a.AuthorId
            where f.FollowingUserId = @Id
            order by a.PublishDate desc
            ";
            await using var connection = Connection;
            return await connection.QueryAsync<ArticleDocument>(query, new {Id = userId});
        }
    }
}