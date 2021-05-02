using System;

namespace Conduit.Api.Features.Articles.Events
{
    public record BodyUpdated(
        string ArticleId,
        string Body,
        DateTime UpdatedAt);
}