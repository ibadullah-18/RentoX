using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Notifications;
using RentoX.Contracts.Notifications;

namespace RentoX.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push-devices")]
public sealed class PushDevicesController(IPushDeviceService pushDeviceService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PushDeviceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PushDeviceResponse>> RegisterAsync(
        RegisterPushDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        PushDeviceResult result = await pushDeviceService.RegisterAsync(
            new RegisterPushDeviceCommand(
                userId, request.Platform, request.DeviceId, request.Token),
            cancellationToken);
        return Ok(Map(result));
    }

    [HttpDelete]
    public async Task<ActionResult<DeactivatePushDeviceResponse>> DeactivateAsync(
        [FromQuery] string deviceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out Guid userId)) { return Unauthorized(); }
        int count = await pushDeviceService.DeactivateAsync(
            userId, deviceId, cancellationToken);
        return Ok(new DeactivatePushDeviceResponse(count));
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }

    private static PushDeviceResponse Map(PushDeviceResult result)
    {
        return new PushDeviceResponse(
            result.Id,
            result.Platform,
            result.DeviceId,
            result.IsActive,
            result.RegisteredAtUtc,
            result.LastSeenAtUtc);
    }
}
