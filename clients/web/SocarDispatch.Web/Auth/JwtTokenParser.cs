using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SocarDispatch.Web.Auth;

public static class JwtTokenParser
{
    /// <summary>
    /// Reads the provided JWT string and parses all claims within it.
    /// Converts the 'role' claim to ClaimTypes.Role for compatibility with the Blazor role-checking mechanism.
    /// </summary>
    /// <param name="jwt">Raw JWT token received from the backend </param>
    /// <returns>List of separated claims</returns>
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return [];
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            if (!handler.CanReadToken(jwt))
            {
                return [];
            }

            var token = handler.ReadJwtToken(jwt);
            var claims = new List<Claim>();

            foreach (var claim in token.Claims)
            {
                // Make role claims fully compatible with Blazor's integrated [Authorize(Roles="Operator")] control.
                if (claim.Type is "role" or ClaimTypes.Role or "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                }
                // Sub / NameIdentifier mapping
                else if (claim.Type is "sub" or JwtRegisteredClaimNames.Sub)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                }
                // Email mapping
                else if (claim.Type is "email" or JwtRegisteredClaimNames.Email)
                {
                    claims.Add(new Claim(ClaimTypes.Email, claim.Value));
                }
                else
                {
                    claims.Add(claim);
                }
            }

            return claims;
        }
        catch
        {
            return [];
        }
    }

    // Checks whether the token has expired.

    public static bool IsTokenExpired(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return true;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            return token.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }
}
