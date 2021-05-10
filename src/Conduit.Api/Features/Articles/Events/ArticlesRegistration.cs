using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    public static class ArticlesRegistration
    {
        public static void Register()
        {
            TypeMap.AddType<ArticlePublished>("ArticlePublished");
            TypeMap.AddType<BodyUpdated>("BodyUpdated");
            TypeMap.AddType<TitleUpdated>("TitleUpdated");
            TypeMap.AddType<DescriptionUpdated>("DescriptionUpdated");
            TypeMap.AddType<ArticleDeleted>("ArticleDeleted");
            TypeMap.AddType<ArticleFavorited>("ArticleFavorited");
            TypeMap.AddType<ArticleUnfavorited>("ArticleUnfavorited");
        }
    }
}