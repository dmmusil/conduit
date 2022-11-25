using System;
using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("TitleUpdated")]
    public record TitleUpdated(
        string ArticleId,
        string Title,
        string TitleSlug,
        DateTime UpdatedAt);
}