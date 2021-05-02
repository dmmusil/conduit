using System.Collections.Generic;

namespace Conduit.Api.Features.Articles
{
    public record CreateEnvelope(CreateArticle Article);

    public record CreateArticle(
        string Title,
        string Description,
        string Body,
        IEnumerable<string>? Tags);
}