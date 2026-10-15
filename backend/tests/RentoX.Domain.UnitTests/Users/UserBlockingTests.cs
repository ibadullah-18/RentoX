using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using Xunit;

namespace RentoX.Domain.UnitTests.Users;

public sealed class UserBlockingTests
{
    private static UserProfile Make() =>
        UserProfile.Create(Guid.NewGuid(), "Test Person", PreferredLanguage.Azerbaijani);

    [Fact]
    public void AnActiveAccountIsBlockedWithTheReason()
    {
        UserProfile profile = Make();

        profile.Block("  Fraud reports  ");

        Assert.Equal(UserStatus.Blocked, profile.Status);
        Assert.Equal("Fraud reports", profile.StatusReason);
    }

    [Fact]
    public void ABlockedAccountCanBeUnblocked()
    {
        UserProfile profile = Make();
        profile.Block("Fraud reports");

        profile.Unblock();

        Assert.Equal(UserStatus.Active, profile.Status);
        Assert.Null(profile.StatusReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AReasonIsRequired(string reason)
    {
        UserProfile profile = Make();

        Assert.Throws<DomainException>(() => profile.Block(reason));
        Assert.Equal(UserStatus.Active, profile.Status);
    }

    [Fact]
    public void AReasonLongerThanAThousandCharactersIsRefused()
    {
        Assert.Throws<DomainException>(() => Make().Block(new string('x', 1001)));
    }

    [Fact]
    public void OnlyBlockedOrSuspendedAccountsAreUnblockedAndDeletedOnesAreNotBlocked()
    {
        Assert.Throws<DomainException>(() => Make().Unblock());

        UserProfile deleted = Make();
        deleted.Anonymize();
        Assert.Throws<DomainException>(() => deleted.Block("Spam"));
    }
}
