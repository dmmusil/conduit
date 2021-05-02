using System;
using System.Collections.Generic;

namespace Conduit.Api.Features.Articles.Events
{
    public record ArticlePublished(
        string ArticleId,
        string Title,
        string TitleSlug,
        string Description,
        string Body,
        string AuthorId,
        string AuthorUsername,
        string? AuthorBio,
        string? AuthorImage,
        DateTime PublishDate,
        IEnumerable<string>? Tags);
}