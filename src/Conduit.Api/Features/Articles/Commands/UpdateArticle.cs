namespace Conduit.Api.Features.Articles.Commands
{
    public record UpdateArticle(
        string? ArticleId,
        string? Title,
        string? Description,
        string? Body);
}