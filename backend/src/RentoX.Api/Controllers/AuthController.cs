using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authentication;
using RentoX.Contracts.Authentication;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    RegistrationOtpService registrationOtpService,
    CompleteRegistrationService completeRegistrationService)
    : ControllerBase
{

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
}