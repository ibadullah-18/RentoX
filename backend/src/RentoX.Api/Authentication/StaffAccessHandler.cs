using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RentoX.Application.Authorization;
using RentoX.Application.Hr;

namespace RentoX.Api.Authentication;

/// <summary>An Admin whose access the owner closed gets nothing from the admin panel's API.</summary>
public sealed class StaffAccessRequirement : IAuthorizationRequirement;

public sealed class StaffAccessHandler(IStaffHrService hr) : AuthorizationHandler<StaffAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StaffAccessRequirement requirement)
    {
        // The owner can never be shut out of his own panel.
        if (context.User.IsInRole(RoleNames.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId) &&
            await hr.IsAccessAllowedAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
