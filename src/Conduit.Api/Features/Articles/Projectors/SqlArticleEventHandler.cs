using System;
using System.Collections.Generic;
using System.Linq;
using Conduit.Api.Features.Articles.Events;
using Dapper;
using Eventuous.Projections.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Conduit.Api.Features.Articles.Projectors
{
    public class SqlArticleEventHandler : SqlServerProjection
    {
        public SqlArticleEventHandler(IConfiguration configuration, string subscriptionId, ILoggerFactory loggerFactory)
            : base(configuration, subscriptionId, loggerFactory)
        {
        }

        protected override IEnumerable<CommandDefinition> GetCommand(object evt)
        {
            return evt switch
            {
                ArticlePublished e => ArrayOf(new ArticleInsertCommand(e).Command, new TagBatchInsert(e).Command),
                ArticleDeleted e => ArrayOf(
                    new CommandDefinition("delete from Tags where ArticleId=@ArticleId", e),
                    new CommandDefinition("delete from Favorites where ArticleId=@ArticleId", e),
                    new CommandDefinition("delete from Articles where ArticleId=@ArticleId", e)),
                ArticleFavorited e => ArrayOf(
                    new CommandDefinition("insert into Favorites (ArticleId, UserId) values (@ArticleId, @UserId)", e),
                    new CommandDefinition("update Articles set FavoriteCount=FavoriteCount+1 where ArticleId=@ArticleId", e)),
                ArticleUnfavorited e => ArrayOf(
                    new CommandDefinition("delete from Favorites where ArticleId=@ArticleId and UserId=@UserId", e),
                    new CommandDefinition("update Articles set FavoriteCount=FavoriteCount-1 where ArticleId=@ArticleId", e)),
                BodyUpdated e => ArrayOf(new CommandDefinition("update Articles set Body=@Body, UpdatedDate=@UpdatedAt where ArticleId=@ArticleId", e)),
                DescriptionUpdated e => ArrayOf(new CommandDefinition("update Articles set Description=@Description, UpdatedDate=@UpdatedAt where ArticleId=@ArticleId", e)),
                TitleUpdated e => ArrayOf(new CommandDefinition("update Articles set Title=@Title, TitleSlug=@TitleSlug, UpdatedDate=@UpdatedAt where ArticleId=@ArticleId", e)),
                _ => Array.Empty<CommandDefinition>()
            };
        }
    }

    public class TagBatchInsert
    {
        public TagBatchInsert(ArticlePublished e)
        {
            const string query = "insert into Tags (Tag, ArticleId) values (@Tag, @ArticleId)";
            Command = new CommandDefinition(query, e.Tags.Select(t => new {Tag = t, e.ArticleId}));
        }

        public CommandDefinition Command { get; }
    }

    public class ArticleInsertCommand
    {
        public ArticleInsertCommand(ArticlePublished e)
        {
            var query = $@"
insert into Articles
(
    ArticleId
    ,Title           
    ,TitleSlug       
    ,Description     
    ,Body            
    ,AuthorId        
    ,AuthorUsername  
    ,AuthorBio       
    ,AuthorImage     
    ,FavoriteCount   
    ,PublishDate     
) values (
    @ArticleId
    ,@Title           
    ,@TitleSlug       
    ,@Description     
    ,@Body            
    ,@AuthorId        
    ,@AuthorUsername  
    ,@AuthorBio       
    ,@AuthorImage     
    ,0   
    ,@PublishDate     
)
";
            Command = new CommandDefinition(query, e);
        }
        public CommandDefinition Command { get; }
    }
    
}