namespace Conduit.Api.Features.Articles
{
    public record Author(
        string Id,
        string Username,
        string? Bio,
        string? Image,
        bool Following);
}