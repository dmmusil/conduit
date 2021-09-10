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
                    new CommandDefinition("delete from Articles where ArticleId=@ArticleId", e)),
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