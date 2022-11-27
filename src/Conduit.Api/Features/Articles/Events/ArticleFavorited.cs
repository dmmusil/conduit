using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("ArticleFavorited")]
    public record ArticleFavorited(string ArticleId, string UserId);
}
