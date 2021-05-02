using System;

namespace Conduit.Api.Features.Articles.Events
{
    public record TitleUpdated(
        string ArticleId,
        string Title,
        string TitleSlug,
        DateTime UpdatedAt);
}