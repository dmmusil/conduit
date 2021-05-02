using System;

namespace Conduit.Api.Features.Articles
{
    public record ArticleResponse(
        string Title,
        string Slug,
        string Description,
        string Body,
        Author Author,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool Favorited,
        int FavoritesCount);
}