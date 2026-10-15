using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;

namespace RentoX.Api.Controllers.Admin;

public sealed record GrantAdminRequest(string PhoneNumber);

public sealed record SetSectionsRequest(IReadOnlyList<string> Sections);

public sealed record EndedSessionsResponse(int Ended);

/// <summary>Who may use the admin panel. For the SuperAdmin only.</summary>
[ApiController]
[Route("api/admin/staff")]
[Authorize(Policy = PolicyNames.AdminAccess, Roles = "SuperAdmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminStaffController(IStaffManagementService staffService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StaffMember>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StaffMember>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await staffService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<StaffMember>(StatusCodes.Status201Created)]
    public async Task<ActionResult<StaffMember>> GrantAsync(
        GrantAdminRequest request,
        CancellationToken cancellationToken)
    {
        StaffMember member = await staffService.GrantAdminAsync(request.PhoneNumber, cancellationToken);
        return Created($"/api/admin/staff/{member.UserId}", member);
    }

    [HttpPut("{userId:guid}/sections")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSectionsAsync(
        Guid userId,
        SetSectionsRequest request,
        CancellationToken cancellationToken)
    {
        await staffService.SetSectionsAsync(userId, request.Sections, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/end-sessions")]
    [ProducesResponseType<EndedSessionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EndedSessionsResponse>> EndSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        int ended = await staffService.EndSessionsAsync(userId, cancellationToken);
        return Ok(new EndedSessionsResponse(ended));
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(Guid userId, CancellationToken cancellationToken)
    {
        await staffService.RevokeAdminAsync(userId, cancellationToken);
        return NoContent();
    }
}
