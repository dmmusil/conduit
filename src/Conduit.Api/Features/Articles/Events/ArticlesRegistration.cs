using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    public static class ArticlesRegistration
    {
        public static void Register()
        {
            TypeMap.AddType<ArticlePublished>("ArticlePublished");
        }
    }
}