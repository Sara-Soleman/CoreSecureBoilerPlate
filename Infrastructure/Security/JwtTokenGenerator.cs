using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security
{
    public class JwtTokenGenerator(ApplicationDbContext dbContext)
    {
        public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_NEVER_SHARE_IT_MAKE_IT_VERY_LONG_1234567890!");//this need to be changed to a secure key in production and stored safely (e.g., in environment variables or a secrets manager)

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = "SecureBankingIssuer",
                Audience = "SecureBankingAudience",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<string> GenerateAndSaveRefreshTokenAsync(Guid userId, string ipAddress, IpLocationResult location)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var tokenString = Convert.ToBase64String(randomNumber);

            var refreshToken = new RefreshToken
            {
                
                Token = tokenString,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress,
                UserId = userId,
               
                Country = location.Country,
                City = location.City,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();

            return tokenString;
        }
    }
}
