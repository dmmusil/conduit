using System;
using Eventuous.Projections.MongoDB.Tools;

namespace Conduit.Api.Features.Articles.Projections
{
    public record ArticleDocument(
        string ArticleId,
        string Title,
        string TitleSlug,
        string Description,
        string Body,
        string AuthorId,
        string AuthorUsername,
        string AuthorBio,
        string AuthorImage,
        DateTime PublishDate,
        DateTime? UpdatedDate) : ProjectedDocument(ArticleId);
}