using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentoX.Domain.Listings;
using RentoX.Domain.Messaging;
using RentoX.Infrastructure.Identity;

namespace RentoX.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration
    : IEntityTypeConfiguration<Conversation>
{
    public void Configure(
        EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "messaging");

        builder.HasKey(conversation => conversation.Id);

        builder.Property(conversation =>
                conversation.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(conversation => new
        {
            conversation.ListingId,
            conversation.BuyerId
        }).IsUnique();

        builder.HasIndex(conversation => new
        {
            conversation.BuyerId,
            conversation.CreatedAtUtc
        });

        builder.HasIndex(conversation => new
        {
            conversation.SellerId,
            conversation.CreatedAtUtc
        });

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(conversation =>
                conversation.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(conversation =>
                conversation.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(conversation =>
                conversation.SellerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}