using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Api.Auth
{
    /// <summary>
    /// Creates insecure auth tokens.
    /// </summary>
    public class JwtIssuer
    {
        private readonly AppSettings _appSettings;

        public JwtIssuer(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        /// <summary>
        /// Generate a token that's valid for 7 days.
        /// </summary>
        /// <param name="id">Stream identifier.</param>
        /// <returns>The token.</returns>
        public string GenerateJwtToken(string id)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {new Claim("id", id)}),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}