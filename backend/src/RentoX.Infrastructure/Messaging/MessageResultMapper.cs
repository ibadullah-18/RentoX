using RentoX.Application.Messaging;
using RentoX.Domain.Messaging;

namespace RentoX.Infrastructure.Messaging;

internal static class MessageResultMapper
{
    public static MessageResult Map(Message message)
    {
        return new MessageResult(
            message.Id, message.ConversationId, message.SenderId,
            message.Body, message.SentAtUtc, message.ReadAtUtc)
        {
            Images = message.Images.OrderBy(image => image.DisplayOrder)
                .Select(image => new MessageImageResult(
                    image.Id,
                    $"/api/conversations/{message.ConversationId:D}/images/{image.Id:D}",
                    image.ContentType, image.SizeBytes, image.DisplayOrder))
                .ToArray()
        };
    }
}
