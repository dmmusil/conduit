using System;
using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("DescriptionUpdated")]
    public record DescriptionUpdated(string ArticleId, string Description, DateTime UpdatedAt);
}
