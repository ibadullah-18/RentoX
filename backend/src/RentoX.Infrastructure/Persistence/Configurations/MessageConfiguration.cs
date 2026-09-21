using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration
    : IEntityTypeConfiguration<Message>
{
    public void Configure(
        EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", "messaging");

        builder.HasKey(message => message.Id);

        builder.HasMany(message => message.Images)
            .WithOne()
            .HasForeignKey(image => image.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(message => message.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(message => message.Body)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(message => message.SentAtUtc)
            .IsRequired();

        builder.HasIndex(message => new
        {
            message.ConversationId,
            message.SentAtUtc,
            message.Id
        });

        builder.HasIndex(message => new
        {
            message.ConversationId,
            message.ReadAtUtc
        });

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(message =>
                message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(message =>
                message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
