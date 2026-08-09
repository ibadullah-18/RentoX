using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Catalog.Fields;
using RentoX.Contracts.Catalog.Fields;
using RentoX.Domain.Users.Enums;

namespace RentoX.Api.Controllers;

[ApiController]
[Route("api/categories/{categoryId:guid}/fields")]
public sealed class CategoryFieldsController(
    ICategoryFieldQueryService queryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<CategoryFieldDefinitionResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<
        IReadOnlyList<CategoryFieldDefinitionResponse>>>
        GetAsync(
            Guid categoryId,
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

        IReadOnlyList<CategoryFieldDefinitionResult> result =
            await queryService.GetForCategoryAsync(
                categoryId,
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

    private static CategoryFieldDefinitionResponse Map(
        CategoryFieldDefinitionResult field)
    {
        CategoryFieldOptionDefinitionResponse[] options =
            field.Options
                .Select(option =>
                    new CategoryFieldOptionDefinitionResponse(
                        option.Id,
                        option.Value,
                        option.Label,
                        option.DisplayOrder))
                .ToArray();

        return new CategoryFieldDefinitionResponse(
            field.Id,
            field.SourceCategoryId,
            field.Key,
            field.Label,
            field.Type,
            field.IsRequired,
            field.IsFilterable,
            field.IsSearchable,
            field.AllowCustomValue,
            field.DisplayOrder,
            options);
    }
}