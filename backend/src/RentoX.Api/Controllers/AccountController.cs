using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Accounts;
using RentoX.Contracts.Accounts;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(
    IAccountProfileService accountProfileService)
    : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>>
        GetCurrentUserAsync(
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        AccountProfileResult? result =
            await accountProfileService.GetAsync(
                userId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(CreateResponse(result));
    }

    [HttpPut("me")]
    [ProducesResponseType<CurrentUserResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>>
        UpdateCurrentUserAsync(
            UpdateAccountProfileRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        AccountProfileResult? result =
            await accountProfileService.UpdateAsync(
                userId,
                request.FullName,
                request.Bio,
                request.PreferredLanguage,
                cancellationToken);

        if (result is null)
        {
            return BadRequest();
        }

        return Ok(CreateResponse(result));
    }

    private bool TryGetUserId(out Guid userId)
    {
        string? userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    private static CurrentUserResponse CreateResponse(
        AccountProfileResult result)
    {
        return new CurrentUserResponse(
            result.UserId,
            result.PhoneNumber,
            result.FullName,
            result.Bio,
            result.PreferredLanguage,
            result.Status);
    }
}