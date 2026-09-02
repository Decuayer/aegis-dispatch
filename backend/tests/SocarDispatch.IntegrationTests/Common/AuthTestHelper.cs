using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SocarDispatch.Domain.Entities;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Common;

public static class AuthTestHelper
{
    private const string DefaultSecretKey = "SOCAR_Super_Secret_Key_For_Emergency_Dispatch_System_2026";
    private const string DefaultIssuer = "socar-dispatch-api";
    private const string DefaultAudience = "socar-dispatch-clients";

    public static string GenerateToken(
        Guid userId,
        string email,
        string fullName,
        RoleType role,
        string department = "HSE",
        string? subRole = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DefaultSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Role, role.ToString()),
            new("department", department)
        };

        if (!string.IsNullOrEmpty(subRole))
        {
            claims.Add(new Claim("sub_role", subRole));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = DefaultIssuer,
            Audience = DefaultAudience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateToken(User user)
    {
        return GenerateToken(
            user.Id,
            user.Email,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.RoleType,
            user.Department,
            user.SubRole);
    }

    public static HttpClient CreateAuthenticatedClient(
        this CustomWebApplicationFactory factory,
        User user)
    {
        var client = factory.CreateClient();
        var token = GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient CreateAuthenticatedClient(
        this CustomWebApplicationFactory factory,
        RoleType role,
        Guid? userId = null,
        string email = "testuser@socar.az",
        string department = "HSE")
    {
        var client = factory.CreateClient();
        var targetUserId = userId ?? Guid.NewGuid();
        var token = GenerateToken(targetUserId, email, "Test User", role, department);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
