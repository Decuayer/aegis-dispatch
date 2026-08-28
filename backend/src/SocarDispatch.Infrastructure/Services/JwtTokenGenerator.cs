using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SocarDispatch.Application.Common.Interfaces;
using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var secretKey = _configuration["JWT_SECRET_KEY"]
            ?? _configuration["JwtSettings:SecretKey"]
            ?? "SOCAR_Super_Secret_Key_For_Emergency_Dispatch_System_2026";

        var issuer = _configuration["JWT_ISSUER"]
            ?? _configuration["JwtSettings:Issuer"]
            ?? "socar-dispatch-api";

        var audience = _configuration["JWT_AUDIENCE"]
            ?? _configuration["JwtSettings:Audience"]
            ?? "socar-dispatch-clients";

        var expiryHours = int.TryParse(_configuration["JWT_EXPIRY_HOURS"], out var hours) ? hours : 24;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, user.RoleType.ToString()),
            new("department", user.Department)
        };

        if (!string.IsNullOrEmpty(user.SubRole))
        {
            claims.Add(new Claim("sub_role", user.SubRole));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAt);
    }
}