using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("UsernameUpdated")]
    public record UsernameUpdated(string StreamId, string Username);
}
