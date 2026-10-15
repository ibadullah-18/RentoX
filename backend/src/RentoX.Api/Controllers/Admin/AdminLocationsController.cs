using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Locations;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Cities and districts for the staff. Needs the catalog section.</summary>
[ApiController]
[Route("api/admin/locations")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.CatalogPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminLocationsController(IAdminLocationService locationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminCityItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminCityItem>>> GetAsync(CancellationToken cancellationToken)
    {
        return Ok(await locationService.GetAllAsync(cancellationToken));
    }

    [HttpPost("cities")]
    [ProducesResponseType<CreatedLocation>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatedLocation>> CreateCityAsync(
        LocationRequest request, CancellationToken cancellationToken)
    {
        Guid id = await locationService.CreateCityAsync(
            request.NameAz, request.NameRu, request.NameEn, request.DisplayOrder, cancellationToken);
        return Ok(new CreatedLocation(id));
    }

    [HttpPut("cities/{cityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCityAsync(
        Guid cityId, LocationRequest request, CancellationToken cancellationToken)
    {
        await locationService.UpdateCityAsync(
            cityId, request.NameAz, request.NameRu, request.NameEn, request.DisplayOrder,
            request.IsActive ?? true, cancellationToken);
        return NoContent();
    }

    [HttpPost("cities/{cityId:guid}/districts")]
    [ProducesResponseType<CreatedLocation>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CreatedLocation>> CreateDistrictAsync(
        Guid cityId, LocationRequest request, CancellationToken cancellationToken)
    {
        Guid id = await locationService.CreateDistrictAsync(
            cityId, request.NameAz, request.NameRu, request.NameEn, request.DisplayOrder, cancellationToken);
        return Ok(new CreatedLocation(id));
    }

    [HttpPut("districts/{districtId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateDistrictAsync(
        Guid districtId, LocationRequest request, CancellationToken cancellationToken)
    {
        await locationService.UpdateDistrictAsync(
            districtId, request.NameAz, request.NameRu, request.NameEn, request.DisplayOrder,
            request.IsActive ?? true, cancellationToken);
        return NoContent();
    }
}

/// <param name="IsActive">On an update: whether the place is offered. Left out on a create (new places are on).</param>
public sealed record LocationRequest(string NameAz, string NameRu, string NameEn, int DisplayOrder, bool? IsActive);

public sealed record CreatedLocation(Guid Id);
