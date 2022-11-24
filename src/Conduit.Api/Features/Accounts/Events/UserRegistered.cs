using Eventuous;

namespace Conduit.Api.Features.Accounts.Events
{
    [EventType("UserRegistered")]
    public record UserRegistered(
        string StreamId,
        string Email,
        string Username,
        string PasswordHash);
}