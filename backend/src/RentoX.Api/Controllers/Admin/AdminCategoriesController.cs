using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Authorization;
using RentoX.Application.Catalog.Categories;
using RentoX.Contracts.Catalog.Categories;

namespace RentoX.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.CatalogPolicy)]
public sealed class AdminCategoriesController(
    ICategoryManagementService categoryManagementService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateCategoryResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateCategoryResponse>>
        CreateAsync(
            CreateCategoryRequest request,
            CancellationToken cancellationToken)
    {
        CategoryTranslationInput[] translations =
            request.Translations
                .Select(translation =>
                    new CategoryTranslationInput(
                        translation.Language,
                        translation.Name))
                .ToArray();

        CreateCategoryCommand command = new(
            request.ParentId,
            request.Slug,
            request.IconUrl,
            request.DisplayOrder,
            translations);

        CreateCategoryResult result =
            await categoryManagementService.CreateAsync(
                command,
                cancellationToken);

        CreateCategoryResponse response = new(
            result.Id,
            result.ParentId,    
            result.Slug,
            result.IconUrl,
            result.DisplayOrder,
            result.IsActive);

        return Created(
            $"/api/admin/categories/{result.Id}",
            response);
    }

    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType<CreateCategoryResponse>(
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateCategoryResponse>>
    UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        CategoryTranslationInput[] translations =
            request.Translations
                .Select(translation =>
                    new CategoryTranslationInput(
                        translation.Language,
                        translation.Name))
                .ToArray();

        UpdateCategoryCommand command = new(
            request.ParentId,
            request.Slug,
            request.IconUrl,
            request.DisplayOrder,
            translations);

        CreateCategoryResult result =
            await categoryManagementService.UpdateAsync(
                categoryId,
                command,
                cancellationToken);

        return Ok(new CreateCategoryResponse(
            result.Id,
            result.ParentId,
            result.Slug,
            result.IconUrl,
            result.DisplayOrder,
            result.IsActive));
    }

    /// <summary>
    /// Which rental periods listings of this category may use (1 hour,
    /// 2 day, 3 week, 4 month, 5 event, 6 negotiable). An empty list means
    /// "the parent's choice, or all".
    /// </summary>
    [HttpPut("{categoryId:guid}/rental-periods")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetRentalPeriodsAsync(
        Guid categoryId,
        SetCategoryRentalPeriodsRequest request,
        CancellationToken cancellationToken)
    {
        await categoryManagementService.SetRentalPeriodsAsync(
            categoryId,
            request.Periods.ToArray(),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Deletes a category that has no listing and no sub-category.</summary>
    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        await categoryManagementService.DeleteAsync(categoryId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{categoryId:guid}/status")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetStatusAsync(
    Guid categoryId,
    SetCategoryStatusRequest request,
    CancellationToken cancellationToken)
    {
        await categoryManagementService.SetActiveStatusAsync(
            categoryId,
            request.IsActive,
            cancellationToken);

        return NoContent();
    }
}