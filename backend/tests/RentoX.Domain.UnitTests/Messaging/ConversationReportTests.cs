using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Messaging;

namespace RentoX.Domain.UnitTests.Messaging;

public sealed class ConversationReportTests
{
    [Fact]
    public void CreateStartsPendingAndNormalizesDetails()
    {
        ConversationReport report = ConversationReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), ConversationReportReason.Spam,
            "  repeated advertising  ", null, DateTimeOffset.UtcNow);

        Assert.Equal(ConversationReportStatus.Pending, report.Status);
        Assert.Equal("repeated advertising", report.Details);
    }

    [Fact]
    public void OtherReasonRequiresDetails()
    {
        Assert.Throws<DomainException>(() => ConversationReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), ConversationReportReason.Other,
            null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompletedReportCannotBeChanged()
    {
        ConversationReport report = ConversationReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), ConversationReportReason.Fraud,
            null, null, DateTimeOffset.UtcNow);
        report.Review(
            ConversationReportStatus.Resolved, Guid.NewGuid(),
            "Checked and action taken.", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => report.Review(
            ConversationReportStatus.Reviewing, Guid.NewGuid(),
            null, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void CompletedStatusRequiresResolutionNote(int status)
    {
        ConversationReport report = ConversationReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), ConversationReportReason.Harassment,
            null, null, DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => report.Review(
            (ConversationReportStatus)status, Guid.NewGuid(),
            " ", DateTimeOffset.UtcNow));
    }
}
