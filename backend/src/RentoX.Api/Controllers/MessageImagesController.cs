using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentoX.Application.Messaging;
using RentoX.Contracts.Messaging;

namespace RentoX.Api.Controllers;

public sealed class SendMessageImagesForm
{
    [StringLength(2000)]
    public string? Body { get; set; }
    public List<IFormFile> Files { get; set; } = [];
}

[ApiController]
[Authorize]
[Route("api/conversations/{conversationId:guid}")]
public sealed class MessageImagesController(IMessageImageService imageService) : ControllerBase
{
    [HttpPost("messages/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(55 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> SendImagesAsync(
        Guid conversationId, [FromForm] SendMessageImagesForm form,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId) || userId == Guid.Empty)
        {
            return Unauthorized();
        }
        if (form.Files is null || form.Files.Count is < 1 or > 5)
        {
            return Problem(statusCode: 400, detail: "A message must contain 1 to 5 images.");
        }
        List<MessageImageUpload> uploads = [];
        try
        {
            foreach (IFormFile file in form.Files)
            {
                uploads.Add(new MessageImageUpload(file.OpenReadStream(), file.ContentType, file.Length));
            }
            MessageResult? result = await imageService.SendImagesAsync(
                userId, conversationId, form.Body, uploads, cancellationToken);
            if (result is null) { return NotFound(); }
            MessageResponse response = new(
                result.Id, result.ConversationId, result.SenderId,
                result.Body, result.SentAtUtc, result.ReadAtUtc)
            {
                Images = result.Images.Select(image => new MessageImageResponse(
                    image.Id, image.Url, image.ContentType, image.SizeBytes, image.DisplayOrder)).ToArray()
            };
            return Created($"/api/conversations/{conversationId:D}/messages", response);
        }
        finally
        {
            foreach (MessageImageUpload upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    [HttpGet("images/{imageId:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DownloadAsync(
        Guid conversationId, Guid imageId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId) || userId == Guid.Empty)
        {
            return Unauthorized();
        }
        MessageImageDownload? image = await imageService.OpenImageAsync(
            userId, conversationId, imageId, cancellationToken);
        if (image is null) { return NotFound(); }
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Content, image.ContentType);
    }
}
