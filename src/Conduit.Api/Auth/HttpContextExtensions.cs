using Conduit.Api.Features.Accounts;
using Microsoft.AspNetCore.Http;

namespace Conduit.Api.Auth
{
    public static class HttpContextExtensions
    {
        public static User GetLoggedInUser(this HttpContext context) =>
            (User)context.Items["User"]!;

        public static User? TryGetLoggedInUser(this HttpContext context) =>
            (User?)context.Items["User"];
    }
}
