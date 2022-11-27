using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("AccountFollowed")]
    public record AccountFollowed(string StreamId, string FollowedId);
}
