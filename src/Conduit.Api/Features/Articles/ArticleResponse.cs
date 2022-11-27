using System;
using System.Collections.Generic;

namespace Conduit.Api.Features.Articles
{
    public record ArticleEnvelope(ArticleResponse Article);

    public record ArticleResponse(
        string Title,
        string Slug,
        string Description,
        string Body,
        Author Author,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool Favorited,
        int FavoritesCount,
        IEnumerable<string> TagList
    );
}
