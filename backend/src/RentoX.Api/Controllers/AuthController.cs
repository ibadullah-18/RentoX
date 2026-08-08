using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RentoX.Application.Authentication;
using RentoX.Contracts.Authentication;
using System.Security.Claims;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RegistrationOtpService registrationOtpService,
    CompleteRegistrationService completeRegistrationService,
    LoginOtpService loginOtpService,
    CompleteLoginService completeLoginService,
    IRefreshTokenService refreshTokenService,
    IAuthSessionService authSessionService)
    : ControllerBase
{

    [AllowAnonymous]
    [EnableRateLimiting("otp-request")]
    [HttpPost("login/otp")]
    [ProducesResponseType<LoginOtpResponse>(
    StatusCodes.Status202Accepted)]
    public async Task<ActionResult<LoginOtpResponse>>
    RequestLoginOtpAsync(
        LoginOtpRequest request,
        CancellationToken cancellationToken)
    {
        LoginOtpResult result =
            await loginOtpService.RequestAsync(
                request.PhoneNumber,
                cancellationToken);

        LoginOtpResponse response = new(
            result.ChallengeId,
            result.ExpiresAtUtc,
            result.ResendAvailableAtUtc);

        return Accepted(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("otp-verification")]
    [HttpPost("login/complete")]
    [ProducesResponseType<CompleteLoginResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CompleteLoginResponse>>
        CompleteLoginAsync(
            CompleteLoginRequest request,
            CancellationToken cancellationToken)
    {
        CompleteLoginResult result =
            await completeLoginService.CompleteAsync(
                request.ChallengeId,
                request.Code,
                cancellationToken);

        CompleteLoginResponse response = new(
            result.UserId,
            result.PhoneNumber,
            result.Tokens.AccessToken,
            result.Tokens.RefreshToken,
            result.Tokens.AccessTokenExpiresAtUtc,
            result.Tokens.RefreshTokenExpiresAtUtc);

        return Ok(response);
    }

    [EnableRateLimiting("otp-request")]
    [HttpPost("registration/otp")]
    [ProducesResponseType(
        typeof(RequestRegistrationOtpResponse),
        StatusCodes.Status202Accepted)]
    public async Task<
        ActionResult<RequestRegistrationOtpResponse>>
        RequestRegistrationOtp(
            RequestRegistrationOtpRequest request,
            CancellationToken cancellationToken)
    {
        RegistrationOtpResult result =
            await registrationOtpService.RequestAsync(
                request.PhoneNumber,
                cancellationToken);

        RequestRegistrationOtpResponse response = new(
            result.ChallengeId,
            result.ExpiresAtUtc,
            result.ResendAvailableAtUtc);

        return Accepted(response);
    }

    [EnableRateLimiting("otp-verification")]
    [HttpPost("registration/complete")]
    [ProducesResponseType(
    typeof(CompleteRegistrationResponse),
    StatusCodes.Status201Created)]
    public async Task<
    ActionResult<CompleteRegistrationResponse>>
    CompleteRegistration(
        CompleteRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        CompleteRegistrationResult result =
            await completeRegistrationService.CompleteAsync(
                request.ChallengeId,
                request.Code,
                request.FullName,
                request.PreferredLanguage,
                cancellationToken);

        CompleteRegistrationResponse response = new(
                result.UserId,
                result.PhoneNumber,
                result.Tokens.AccessToken,
                result.Tokens.RefreshToken,
                result.Tokens.AccessTokenExpiresAtUtc,
                result.Tokens.RefreshTokenExpiresAtUtc);

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [EnableRateLimiting("token")]
    [HttpPost("refresh")]
    [ProducesResponseType<RefreshTokenResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RefreshTokenResponse>>
    RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        AuthTokenResult? result =
            await refreshTokenService.RefreshAsync(
                request.RefreshToken,
                cancellationToken);

        if (result is null)
        {
            return Unauthorized();
        }

        RefreshTokenResponse response = new(
            result.AccessToken,
            result.RefreshToken,
            result.AccessTokenExpiresAtUtc,
            result.RefreshTokenExpiresAtUtc);

        return Ok(response);
    }

    [Authorize]
    [EnableRateLimiting("token")]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(
    LogoutRequest request,
    CancellationToken cancellationToken)
    {
        bool revoked =
            await authSessionService.RevokeAsync(
                request.RefreshToken,
                cancellationToken);

        if (!revoked)
        {
            return Unauthorized();
        }

        return NoContent();
    }

    [Authorize]
    [EnableRateLimiting("token")]
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAllAsync(
    CancellationToken cancellationToken)
    {
        string? userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Unauthorized();
        }

        await authSessionService.RevokeAllAsync(
            userId,
            cancellationToken);

        return NoContent();
    }
}