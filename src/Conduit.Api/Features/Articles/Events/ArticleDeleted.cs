using System.Collections.Generic;

namespace Conduit.Api.Features.Articles.Events
{
    public record ArticleDeleted(string ArticleId, IEnumerable<string>? Tags);
}