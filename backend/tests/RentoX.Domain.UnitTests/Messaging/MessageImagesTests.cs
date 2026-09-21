using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;

namespace RentoX.Domain.UnitTests.Messaging;

public sealed class MessageImagesTests
{
    private static readonly int[] ExpectedImageOrder = [0, 1, 2, 3, 4];

    [Fact]
    public void ImageMessageAllowsEmptyTextAndKeepsImageOrder()
    {
        Message message = Message.CreateWithImages(Guid.NewGuid(), Guid.NewGuid(), null,
            DateTimeOffset.UtcNow,
            [new("chat-attachments/a.jpg", "image/jpeg", 100),
             new("chat-attachments/b.png", "image/png", 200)]);
        Assert.Empty(message.Body);
        Assert.Collection(message.Images,
            image => { Assert.Equal(0, image.DisplayOrder); Assert.Equal(message.Id, image.MessageId); },
            image => { Assert.Equal(1, image.DisplayOrder); Assert.Equal(message.Id, image.MessageId); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ImageCountOutsideLimitIsRejected(int count)
    {
        MessageImageFile[] images = Enumerable.Range(0, count)
            .Select(index => new MessageImageFile($"chat-attachments/{index}.jpg", "image/jpeg", 100))
            .ToArray();
        Assert.Throws<DomainException>(() => Message.CreateWithImages(
            Guid.NewGuid(), Guid.NewGuid(), "Photo", DateTimeOffset.UtcNow, images));
    }

    [Fact]
    public void FiveImagesAreAccepted()
    {
        MessageImageFile[] images = Enumerable.Range(0, 5)
            .Select(index => new MessageImageFile($"chat-attachments/{index}.jpg", "image/jpeg", 100))
            .ToArray();
        Message message = Message.CreateWithImages(
            Guid.NewGuid(), Guid.NewGuid(), "Photo", DateTimeOffset.UtcNow, images);
        Assert.Equal(ExpectedImageOrder, message.Images.Select(image => image.DisplayOrder));
    }

    [Fact]
    public void TextMessageStillRequiresText()
    {
        Assert.Throws<DomainException>(() => Message.Create(
            Guid.NewGuid(), Guid.NewGuid(), " ", DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(10485761L)]
    public void InvalidImageSizeIsRejected(long size)
    {
        Assert.Throws<DomainException>(() => Message.CreateWithImages(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
            [new("chat-attachments/a.jpg", "image/jpeg", size)]));
    }

    [Fact]
    public void SvgImageIsRejected()
    {
        Assert.Throws<DomainException>(() => Message.CreateWithImages(
            Guid.NewGuid(), Guid.NewGuid(), null, DateTimeOffset.UtcNow,
            [new("chat-attachments/a.svg", "image/svg+xml", 100)]));
    }
}
