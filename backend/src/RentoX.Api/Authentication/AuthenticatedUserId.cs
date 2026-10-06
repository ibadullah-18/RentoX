using System.Security.Claims;

namespace RentoX.Api.Authentication;

public static class AuthenticatedUserId
{
    public static Guid? Resolve(ClaimsPrincipal? principal)
    {
        if (principal is null ||
            principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        Guid? result = null;

        foreach (Claim claim in principal.Claims)
        {
            if (claim.Type != ClaimTypes.NameIdentifier &&
                claim.Type != "sub")
            {
                continue;
            }

            if (!Guid.TryParse(claim.Value, out Guid candidate) ||
                candidate == Guid.Empty)
            {
                return null;
            }

            if (result.HasValue && result.Value != candidate)
            {
                return null;
            }

            result = candidate;
        }

        return result;
    }
}
