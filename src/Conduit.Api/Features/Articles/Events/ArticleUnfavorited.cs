using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("ArticleUnfavorited")]
    public record ArticleUnfavorited(string ArticleId, string UserId);
}