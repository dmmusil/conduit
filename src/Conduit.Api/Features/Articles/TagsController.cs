using System.Collections.Generic;
using System.Threading.Tasks;
using Conduit.Api.Features.Articles.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Articles
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        private readonly ArticleRepository _articles;

        public TagsController(ArticleRepository articles) => _articles = articles;

        [HttpGet]
        public async Task<IActionResult> Get() => Ok(new TagsEnvelope(await _articles.GetTags()));
    }

    public record TagsEnvelope(IEnumerable<string> Tags);
}
