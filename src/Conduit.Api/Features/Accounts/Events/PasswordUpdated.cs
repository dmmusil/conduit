using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("PasswordUpdated")]
    public record PasswordUpdated(string StreamId, string PasswordHash);
}
