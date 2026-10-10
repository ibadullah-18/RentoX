using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Accounts;
using RentoX.Contracts.Accounts;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(
    IAccountProfileService accountProfileService,
    IAccountDeletionService accountDeletionService)
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

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId))
        {
            return Unauthorized();
        }

        if (!request.Confirm)
        {
            return BadRequest("Deleting the account must be confirmed.");
        }

        bool deleted =
            await accountDeletionService.DeleteAsync(
                userId,
                cancellationToken);

        return deleted ? NoContent() : NotFound();
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
        Guid? resolvedUserId =
            RentoX.Api.Authentication.AuthenticatedUserId.Resolve(User);

        userId = resolvedUserId.GetValueOrDefault();

        return resolvedUserId.HasValue;
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
            result.Status)
        {
            ProfileImageUrl = result.ProfileImageUrl
        };
    }
}
