using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Catalog.Categories;
using RentoX.Contracts.Catalog.Categories;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(
    ICategoryQueryService categoryQueryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<CategoryTreeResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<
        ActionResult<IReadOnlyList<CategoryTreeResponse>>>
        GetTreeAsync(
            [FromQuery] string language = "az",
            CancellationToken cancellationToken = default)
    {
        PreferredLanguage? preferredLanguage =
            ParseLanguage(language);

        if (!preferredLanguage.HasValue)
        {
            return BadRequest(
                "Language must be az, ru or en.");
        }

        IReadOnlyList<CategoryTreeResult> result =
            await categoryQueryService.GetTreeAsync(
                preferredLanguage.Value,
                cancellationToken);

        return Ok(result.Select(Map).ToArray());
    }

    private static PreferredLanguage? ParseLanguage(
        string language)
    {
        return language.Trim().ToLowerInvariant() switch
        {
            "az" => PreferredLanguage.Azerbaijani,
            "ru" => PreferredLanguage.Russian,
            "en" => PreferredLanguage.English,
            _ => null
        };
    }

    private static CategoryTreeResponse Map(
        CategoryTreeResult category)
    {
        return new CategoryTreeResponse(
            category.Id,
            category.ParentId,
            category.Slug,
            category.Name,
            category.IconUrl,
            category.DisplayOrder,
            category.Children.Select(Map).ToArray());
    }
}