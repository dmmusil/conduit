namespace Conduit.Api.Features.Accounts
{
    public record UserUpdate(
        string? Email = null,
        string? Username = null,
        string? Password = null,
        string? Bio = null,
        string? Image = null);
}