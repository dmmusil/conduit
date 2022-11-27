using System.Text.Json.Serialization;

namespace Conduit.Api.Features.Accounts
{
    public record User(
        string Id,
        string Email,
        string Username,
        string? Bio,
        string? Image,
        string? Token = null
    )
    {
        [JsonConstructor]
        public User(string id, string email, string username, string? bio, string? image)
            : this(id, email, username, bio, image, null)
        {
            Id = id;
            Email = email;
            Username = username;
            Bio = bio;
            Image = image;
        }
    }
}
