using System.Collections.Generic;

namespace Conduit.Api.Features.Articles.Commands
{
    public record PublishArticle(
        string Title,
        string TitleSlug,
        string Description,
        string Body,
        Author Author,
        IEnumerable<string>? Tags = null);
}