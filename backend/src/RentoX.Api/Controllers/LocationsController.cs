using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Locations;
using RentoX.Contracts.Locations;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

/// <summary>Cities and districts to place listings in.</summary>
[ApiController]
[Route("api/locations")]
public sealed class LocationsController(
    ILocationQueryService locationQueryService)
    : ControllerBase
{
    [HttpGet("cities")]
    [ProducesResponseType<IReadOnlyList<CityResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>>
        GetCitiesAsync(
            [FromQuery] string language = "az",
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferred = ParseLanguage(language);

        if (!preferred.HasValue)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        IReadOnlyList<CityResult> cities =
            await locationQueryService.GetCitiesAsync(
                preferred.Value,
                cancellationToken);

        return Ok(cities
            .Select(city => new CityResponse(
                city.Id,
                city.Name,
                city.HasDistricts))
            .ToList());
    }

    [HttpGet("cities/{cityId:guid}/districts")]
    [ProducesResponseType<IReadOnlyList<DistrictResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DistrictResponse>>>
        GetDistrictsAsync(
            Guid cityId,
            [FromQuery] string language = "az",
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferred = ParseLanguage(language);

        if (!preferred.HasValue)
        {
            return BadRequest("Language must be az, ru or en.");
        }

        IReadOnlyList<DistrictResult>? districts =
            await locationQueryService.GetDistrictsAsync(
                cityId,
                preferred.Value,
                cancellationToken);

        if (districts is null)
        {
            return NotFound();
        }

        return Ok(districts
            .Select(district => new DistrictResponse(
                district.Id,
                district.Name))
            .ToList());
    }

    private static PreferredLanguage? ParseLanguage(string language) =>
        language.Trim().ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
}
