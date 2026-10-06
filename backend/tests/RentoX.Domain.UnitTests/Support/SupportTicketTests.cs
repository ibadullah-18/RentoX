using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Support;

namespace RentoX.Domain.UnitTests.Support;

public sealed class SupportTicketTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(
            2026,
            10,
            3,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public void CreateShouldAddInitialUserMessage()
    {
        Guid userId = Guid.NewGuid();

        SupportTicket ticket =
            SupportTicket.Create(
                userId,
                SupportTicketCategory.Technical,
                "  Tətbiq açılmır  ",
                "  Tətbiqə daxil ola bilmirəm.  ",
                CreatedAtUtc);

        Assert.Equal(userId, ticket.UserId);

        Assert.Equal(
            SupportTicketCategory.Technical,
            ticket.Category);

        Assert.Equal(
            "Tətbiq açılmır",
            ticket.Subject);

        Assert.Equal(
            SupportTicketPriority.Normal,
            ticket.Priority);

        Assert.Equal(
            SupportTicketStatus.Open,
            ticket.Status);

        SupportTicketMessage message =
            Assert.Single(ticket.Messages);

        Assert.Equal(userId, message.SenderId);
        Assert.False(message.IsAdmin);

        Assert.Equal(
            "Tətbiqə daxil ola bilmirəm.",
            message.Body);
    }

    [Fact]
    public void AdminMessageShouldMoveOpenTicketToInProgress()
    {
        SupportTicket ticket =
            CreateTicket();

        Guid adminId = Guid.NewGuid();
        DateTimeOffset replyTime =
            CreatedAtUtc.AddMinutes(5);

        ticket.AddAdminMessage(
            adminId,
            "Problemi yoxlayırıq.",
            replyTime);

        Assert.Equal(
            SupportTicketStatus.InProgress,
            ticket.Status);

        Assert.Equal(
            replyTime,
            ticket.UpdatedAtUtc);

        SupportTicketMessage message =
            ticket.Messages.Last();

        Assert.Equal(adminId, message.SenderId);
        Assert.True(message.IsAdmin);
    }

    [Fact]
    public void UserMessageShouldReopenResolvedTicket()
    {
        SupportTicket ticket =
            CreateTicket();

        DateTimeOffset resolvedAtUtc =
            CreatedAtUtc.AddMinutes(10);

        ticket.ChangeStatus(
            SupportTicketStatus.Resolved,
            resolvedAtUtc);

        ticket.AddUserMessage(
            ticket.UserId,
            "Problem hələ də davam edir.",
            resolvedAtUtc.AddMinutes(2));

        Assert.Equal(
            SupportTicketStatus.Open,
            ticket.Status);

        Assert.Null(ticket.ResolvedAtUtc);
    }

    [Fact]
    public void ClosedTicketShouldRejectNewMessages()
    {
        SupportTicket ticket =
            CreateTicket();

        ticket.ChangeStatus(
            SupportTicketStatus.Closed,
            CreatedAtUtc.AddMinutes(10));

        Assert.Throws<DomainException>(
            () => ticket.AddUserMessage(
                ticket.UserId,
                "Yeni mesaj",
                CreatedAtUtc.AddMinutes(11)));

        Assert.Throws<DomainException>(
            () => ticket.AddAdminMessage(
                Guid.NewGuid(),
                "Admin mesajı",
                CreatedAtUtc.AddMinutes(11)));
    }

    [Fact]
    public void PriorityShouldBeChanged()
    {
        SupportTicket ticket =
            CreateTicket();

        DateTimeOffset changedAtUtc =
            CreatedAtUtc.AddMinutes(3);

        ticket.ChangePriority(
            SupportTicketPriority.Urgent,
            changedAtUtc);

        Assert.Equal(
            SupportTicketPriority.Urgent,
            ticket.Priority);

        Assert.Equal(
            changedAtUtc,
            ticket.UpdatedAtUtc);
    }

    [Fact]
    public void DifferentUserShouldNotAddOwnerMessage()
    {
        SupportTicket ticket =
            CreateTicket();

        Assert.Throws<DomainException>(
            () => ticket.AddUserMessage(
                Guid.NewGuid(),
                "Başqa istifadəçinin mesajı.",
                CreatedAtUtc.AddMinutes(1)));
    }

    private static SupportTicket CreateTicket()
    {
        return SupportTicket.Create(
            Guid.NewGuid(),
            SupportTicketCategory.General,
            "Kömək lazımdır",
            "Müraciətin ilk mesajı.",
            CreatedAtUtc);
    }
}