namespace Conduit.Api.Features.Accounts.Events
{
        public record UserRegistered(
            string StreamId,
            string Email,
            string Username,
            string PasswordHash);
}