using System.Collections.Generic;
using System.Linq;
using Eventuous.Projections.MongoDB.Tools;

namespace Conduit.Api.Features.Accounts.Projections
{
        public record UserDocument(
            string StreamId,
            string Email,
            string Username,
            string PasswordHash,
            string? Bio,
            string? Image,
            IEnumerable<string> Following) : ProjectedDocument(StreamId)
        {
            public bool VerifyPassword(string password) =>
                BCrypt.Net.BCrypt.Verify(password, PasswordHash);

            public bool IsFollowing(string authorId) =>
                Following.Contains(authorId);
        }
}