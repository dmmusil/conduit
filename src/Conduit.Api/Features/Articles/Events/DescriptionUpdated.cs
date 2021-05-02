using System;

namespace Conduit.Api.Features.Articles.Events
{
    public record DescriptionUpdated(
        string ArticleId,
        string Description,
        DateTime UpdatedAt);
}