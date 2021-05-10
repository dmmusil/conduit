using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Features.Articles
{
    [ApiController]
    [Route("api/tags")]
    public class TagsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new {Tags = new[] {"c#", "react"}});
    }
}