using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conduit.Api.Features.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Api.Auth
{
    /// <summary>
    /// Middleware that checks for Authorization header and attaches account to current request
    /// if existing header contains a valid token.
    /// </summary>
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;

        public JwtMiddleware(RequestDelegate next, AppSettings appSettings)
        {
            _next = next;
            _appSettings = appSettings;
        }

        public async Task Invoke(HttpContext context, UserService userService)
        {
            var token = context.Request.Headers["Authorization"]
                .FirstOrDefault()
                ?.Split(" ")
                .Last();

            if (token != null)
            {
                await AttachUserToContext(context, userService, token);
            }

            await _next(context);
        }

        private async Task AttachUserToContext(
            HttpContext context,
            UserService userService,
            string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    },
                    out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == "id").Value;
                // attach user to context o successful jwt validation
                var account = await userService.Load(userId);
                context.Items["User"] = account;
            }
            catch
            {
                // ignored, user not granted access
            }
        }
    }
}