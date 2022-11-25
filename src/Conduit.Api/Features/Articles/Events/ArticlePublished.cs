using System;
using System.Collections.Generic;
using Eventuous;

namespace Conduit.Api.Features.Articles.Events
{
    [EventType("ArticlePublished")]
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
        IEnumerable<string> Tags);
}