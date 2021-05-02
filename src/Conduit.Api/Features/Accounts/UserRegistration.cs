namespace Conduit.Api.Features.Accounts
{
    public record UserRegistration(
        string Email,
        string Username,
        string Password);
}