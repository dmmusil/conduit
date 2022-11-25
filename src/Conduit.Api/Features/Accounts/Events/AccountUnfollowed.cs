using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("AccountUnfollowed")]
    public record AccountUnfollowed(string StreamId, string UnfollowedId);
}