using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Catalog.Fields;
using RentoX.Contracts.Catalog.Fields;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories/{categoryId:guid}/fields")]
[Authorize(Policy = PolicyNames.AdminAccess)]
public sealed class AdminCategoryFieldsController(
    ICategoryFieldManagementService managementService,
    ICategoryFieldAdministrationService administrationService,
    ICategoryFieldOptionManagementService optionManagementService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CategoryFieldManagementResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<CategoryFieldManagementResponse>>
        CreateAsync(
            Guid categoryId,
            CreateCategoryFieldRequest request,
            CancellationToken cancellationToken)
    {
        FieldTranslationInput[] translations =
            request.Translations
                .Select(MapTranslation)
                .ToArray();

        FieldOptionInput[] options =
            request.Options
                .Select(option =>
                    new FieldOptionInput(
                        option.Value,
                        option.DisplayOrder,
                        option.Translations
                            .Select(MapTranslation)
                            .ToArray()))
                .ToArray();

        CreateCategoryFieldCommand command = new(
            categoryId,
            request.Key,
            request.Type,
            request.IsRequired,
            request.IsFilterable,
            request.IsSearchable,
            request.AllowCustomValue,
            request.AppliesToDescendants,
            request.DisplayOrder,
            translations,
            options);

        CategoryFieldManagementResult result =
            await managementService.CreateAsync(
                command,
                cancellationToken);

        CategoryFieldManagementResponse response =
            MapResponse(result);

        return Created(
            $"/api/admin/categories/{categoryId}/fields/{result.Id}",
            response);
    }

    private static FieldTranslationInput MapTranslation(
        FieldTranslationRequest translation)
    {
        return new FieldTranslationInput(
            translation.Language,
            translation.Label);
    }

    private static CategoryFieldManagementResponse MapResponse(
        CategoryFieldManagementResult result)
    {
        CategoryFieldOptionResponse[] options =
            result.Options
                .Select(option =>
                    new CategoryFieldOptionResponse(
                        option.Id,
                        option.Value,
                        option.DisplayOrder,
                        option.IsActive))
                .ToArray();

        return new CategoryFieldManagementResponse(
            result.Id,
            result.CategoryId,
            result.Key,
            result.Type,
            result.IsRequired,
            result.IsFilterable,
            result.IsSearchable,
            result.AllowCustomValue,
            result.AppliesToDescendants,
            result.DisplayOrder,
            result.IsActive,
            options);
    }

    [HttpPut("{fieldId:guid}")]
    [ProducesResponseType<CategoryFieldManagementResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    public async Task<
    ActionResult<CategoryFieldManagementResponse>>
    UpdateAsync(
        Guid categoryId,
        Guid fieldId,
        UpdateCategoryFieldRequest request,
        CancellationToken cancellationToken)
    {
        FieldTranslationInput[] translations =
            request.Translations
                .Select(MapTranslation)
                .ToArray();

        UpdateCategoryFieldCommand command = new(
            request.Key,
            request.Type,
            request.IsRequired,
            request.IsFilterable,
            request.IsSearchable,
            request.AllowCustomValue,
            request.AppliesToDescendants,
            request.DisplayOrder,
            translations);

        CategoryFieldManagementResult result =
            await administrationService.UpdateAsync(
                fieldId,
                command,
                cancellationToken);

        if (result.CategoryId != categoryId)
        {
            return BadRequest(
                "Field does not belong to this category.");
        }

        return Ok(MapResponse(result));
    }

    [HttpPatch("{fieldId:guid}/status")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetStatusAsync(
    Guid categoryId,
    Guid fieldId,
    SetCategoryFieldStatusRequest request,
    CancellationToken cancellationToken)
    {
        await administrationService.SetActiveStatusAsync(
            fieldId,
            request.IsActive,
            cancellationToken);

        return NoContent();
    }
    [HttpPost("{fieldId:guid}/options")]
    public async Task<ActionResult<CategoryFieldOptionResponse>>
    CreateOptionAsync(
        Guid categoryId,
        Guid fieldId,
        SaveCategoryFieldOptionRequest request,
        CancellationToken cancellationToken)
    {
        SaveCategoryFieldOptionCommand command = new(
            request.Value,
            request.DisplayOrder,
            request.Translations
                .Select(MapTranslation)
                .ToArray());

        CategoryFieldOptionResult result =
            await optionManagementService.CreateAsync(
                categoryId,
                fieldId,
                command,
                cancellationToken);

        return Created(
            $"/api/admin/categories/{categoryId}/fields/{fieldId}/options/{result.Id}",
            MapOptionResponse(result));
    }

    [HttpPut("{fieldId:guid}/options/{optionId:guid}")]
    public async Task<ActionResult<CategoryFieldOptionResponse>>
    UpdateOptionAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        SaveCategoryFieldOptionRequest request,
        CancellationToken cancellationToken)
    {
        SaveCategoryFieldOptionCommand command = new(
            request.Value,
            request.DisplayOrder,
            request.Translations
                .Select(MapTranslation)
                .ToArray());

        CategoryFieldOptionResult result =
            await optionManagementService.UpdateAsync(
                categoryId,
                fieldId,
                optionId,
                command,
                cancellationToken);

        return Ok(MapOptionResponse(result));
    }

    [HttpPatch(
        "{fieldId:guid}/options/{optionId:guid}/status")]
    public async Task<IActionResult> SetOptionStatusAsync(
        Guid categoryId,
        Guid fieldId,
        Guid optionId,
        SetCategoryFieldOptionStatusRequest request,
        CancellationToken cancellationToken)
    {
        await optionManagementService.SetActiveStatusAsync(
            categoryId,
            fieldId,
            optionId,
            request.IsActive,
            cancellationToken);

        return NoContent();
    }



    private static CategoryFieldOptionResponse MapOptionResponse(
    CategoryFieldOptionResult result)
    {
        return new CategoryFieldOptionResponse(
            result.Id,
            result.Value,
            result.DisplayOrder,
            result.IsActive);
    }
}