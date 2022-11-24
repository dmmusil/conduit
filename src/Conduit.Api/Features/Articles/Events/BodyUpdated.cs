using System;
using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("BodyUpdated")]
    public record BodyUpdated(
        string ArticleId,
        string Body,
        DateTime UpdatedAt);
}