using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Admin;
using RentoX.Application.Authorization;

namespace RentoX.Api.Controllers.Admin;

/// <summary>Words that mark a listing for the moderators. Needs the listings section.</summary>
[ApiController]
[Route("api/admin/banned-words")]
[Authorize(Policy = PolicyNames.AdminAccess)]
[Authorize(Policy = AdminSections.ListingsPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminBannedWordsController(IBannedWordService bannedWordService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BannedWordItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BannedWordItem>>> ListAsync(CancellationToken cancellationToken)
    {
        return Ok(await bannedWordService.ListAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<BannedWordItem>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BannedWordItem>> AddAsync(
        AddBannedWordRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await bannedWordService.AddAsync(request.Word, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        return await bannedWordService.RemoveAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>The banned words found in the given listings (comma separated ids, up to 50).</summary>
    [HttpGet("flags")]
    [ProducesResponseType<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<Guid, IReadOnlyList<string>>>> FlagsAsync(
        [FromQuery] string ids,
        CancellationToken cancellationToken)
    {
        List<Guid> parsed = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Guid.TryParse(part, out Guid id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        return Ok(await bannedWordService.FindInListingsAsync(parsed, cancellationToken));
    }
}

public sealed record AddBannedWordRequest(string Word);
