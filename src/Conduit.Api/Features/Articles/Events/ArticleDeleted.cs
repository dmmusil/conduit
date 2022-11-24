using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("ArticleDeleted")]
    public record ArticleDeleted(string ArticleId);
}