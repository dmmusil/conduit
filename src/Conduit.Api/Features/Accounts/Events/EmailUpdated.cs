using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("EmailUpdated")]
    public record EmailUpdated(string StreamId, string Email);
}