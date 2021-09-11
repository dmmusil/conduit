using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        DateTime? UpdatedDate,
        int FavoriteCount,
        IEnumerable<string> TagList)
    {
        [JsonConstructor]
        public ArticleDocument(string articleId, string title, string titleSlug, string description, string body,
            string authorId, string authorUsername, string authorBio, string authorImage, DateTime publishDate,
            DateTime? updatedDate, int favoriteCount) : this(articleId, title, titleSlug, description, body, authorId,
            authorUsername, authorBio, authorImage, publishDate, updatedDate, favoriteCount, new List<string>())
        {
            ArticleId = articleId;
            Title = title;
            TitleSlug = titleSlug;
            Description = description;
            Body = body;
            AuthorId = authorId;
            AuthorUsername = authorUsername;
            AuthorBio = authorBio;
            AuthorImage = authorImage;
            PublishDate = publishDate;
            UpdatedDate = updatedDate;
            FavoriteCount = favoriteCount;
        }
    }
}