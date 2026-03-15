using InitialAspireProject.ApiIdentity.Repository;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InitialAspireProject.ApiIdentity
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(ApplicationUser user, IList<string> roles, IList<Claim>? permissionClaims = null)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            if (permissionClaims is not null)
            {
                foreach (var claim in permissionClaims)
                    claims.Add(claim);
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
