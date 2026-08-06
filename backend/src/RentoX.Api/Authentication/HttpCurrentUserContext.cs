using System.Security.Claims;
using RentoX.Application.Abstractions.Authentication;

namespace RentoX.Api.Authentication;

public sealed class HttpCurrentUserContext(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid? UserId
    {
        get
        {
            string? value = httpContextAccessor.HttpContext?
                .User.FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                value = httpContextAccessor.HttpContext?
                    .User.FindFirst("sub")?
                    .Value;
            }

            return Guid.TryParse(value, out Guid userId)
                ? userId
                : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?
            .User.Identity?
            .IsAuthenticated == true;
}