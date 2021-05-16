using System.Collections.Generic;
using Eventuous.Projections.MongoDB.Tools;

namespace Conduit.Api.Features.Articles.Projections
{
    public record TagDocument
        (Dictionary<string, int> Tags) : ProjectedDocument(nameof(TagDocument));
}