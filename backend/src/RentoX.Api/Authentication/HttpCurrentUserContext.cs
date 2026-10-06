using RentoX.Application.Abstractions.Authentication;

namespace RentoX.Api.Authentication;

public sealed class HttpCurrentUserContext(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid? UserId =>
        AuthenticatedUserId.Resolve(
            httpContextAccessor.HttpContext?.User);

    public bool IsAuthenticated => UserId.HasValue;
}
