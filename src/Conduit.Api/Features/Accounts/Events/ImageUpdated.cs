using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("ImageUpdated")]
    public record ImageUpdated(string StreamId, string Image);
}
