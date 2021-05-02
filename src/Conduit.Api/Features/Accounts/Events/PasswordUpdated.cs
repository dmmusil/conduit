namespace Conduit.Api.Features.Accounts.Events
{
    public record PasswordUpdated(string StreamId, string PasswordHash);
}