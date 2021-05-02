using System.Collections.Generic;

namespace Conduit.Api.Features.Articles
{
    public static class Requests
    {
        public record CreateArticle(
            string Title,
            string Description,
            string Body,
            IEnumerable<string>? Tags);
    }
}