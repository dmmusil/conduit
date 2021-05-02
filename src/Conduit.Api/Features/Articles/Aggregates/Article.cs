using System;
using System.Collections.Generic;
using Conduit.Api.Features.Articles.Events;
using Eventuous;

namespace Conduit.Api.Features.Articles.Aggregates
{
    public record ArticleId(string Value) : AggregateId(Value);

    public class Article : Aggregate<ArticleState, ArticleId>
    {
        public void Publish(
            string title,
            string titleSlug,
            string description,
            string body,
            Author author,
            IEnumerable<string>? tags)
        {
            EnsureDoesntExist();
            var articleId = new ArticleId(Guid.NewGuid().ToString("N"));
            Apply(
                new ArticlePublished(
                    articleId,
                    title,
                    titleSlug,
                    description,
                    body,
                    author.Id,
                    author.Username,
                    author.Bio,
                    author.Image,
                    DateTime.UtcNow,
                    tags));
        }

        public void Update(string? title, string? description, string? body)
        {
            EnsureExists();
            var updatedAt = DateTime.UtcNow;
            if (title != null)
                Apply(
                    new TitleUpdated(
                        State.Id,
                        title,
                        title.ToSlug(),
                        updatedAt));
            if (description != null)
                Apply(new DescriptionUpdated(State.Id, description, updatedAt));
            if (body != null) Apply(new BodyUpdated(State.Id, body, updatedAt));
        }

        public void Delete()
        {
            Apply(new ArticleDeleted(State.Id));
        }
    }

    public record ArticleState : AggregateState<ArticleState, ArticleId>
    {
        public override ArticleState When(object @event) =>
            @event switch
            {
                ArticlePublished(var articleId, var title, var titleSlug, var
                    description, var body, var authorId, var authorUsername, var
                    authorBio, var authorImage, var publishDate, var tags) =>
                    this with
                    {
                        Id = new ArticleId(articleId),
                        Title = title,
                        Slug = titleSlug,
                        Description = description,
                        Body = body,
                        Author = new Author(
                            authorId,
                            authorUsername,
                            authorBio,
                            authorImage,
                            false),
                        CreatedAt = publishDate,
                        Tags = tags
                    },
                TitleUpdated(_, var title, var titleSlug, var updated) => this
                    with
                    {
                        Title = title, Slug = titleSlug, UpdatedAt = updated
                    },
                BodyUpdated(_, var body, var updated) => this with
                {
                    Body = body, UpdatedAt = updated
                },
                DescriptionUpdated(_, var description, var updated) => this with
                {
                    Description = description, UpdatedAt = updated
                },
                _ => this
            };

        public IEnumerable<string>? Tags { get; init; }
        public DateTime CreatedAt { get; private init; }
        public DateTime? UpdatedAt { get; private init; }
        public Author Author { get; private init; } = null!;
        public string Body { get; private init; } = null!;
        public string Description { get; private init; } = null!;
        public string Slug { get; private init; } = null!;
        public string Title { get; private init; } = null!;
        public int FavoriteCount { get; init; }
    }
}