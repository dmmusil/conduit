using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    public static class Registration
    {
        public static void Register()
        {
            TypeMap.AddType<ArticlePublished>("ArticlePublished");
        }
    }
}