namespace Conduit.Api.Features.Accounts
{
    public record User(
        string Id,
        string Email,
        string Username,
        string? Bio = null,
        string? Image = null,
        string? Token = null);
}