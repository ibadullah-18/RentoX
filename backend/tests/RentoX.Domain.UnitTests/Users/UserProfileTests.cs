using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Users.Events;

namespace RentoX.Domain.UnitTests.Users;

public sealed class UserProfileTests
{
    [Fact]
    public void CreateShouldBuildActiveProfile()
    {
        Guid userId = Guid.NewGuid();

        UserProfile profile = UserProfile.Create(
            userId,
            "Ibadulla Huseynzade",
            PreferredLanguage.Azerbaijani);

        Assert.Equal(userId, profile.Id);
        Assert.Equal("Ibadulla Huseynzade", profile.FullName);
        Assert.Equal(UserStatus.Active, profile.Status);
        Assert.Contains(
            profile.DomainEvents,
            domainEvent =>
                domainEvent is UserProfileCreatedDomainEvent);
    }

    [Fact]
    public void CreateShouldRejectEmptyUserId()
    {
        Assert.Throws<DomainException>(() =>
            UserProfile.Create(
                Guid.Empty,
                "Ibadulla Huseynzade",
                PreferredLanguage.Azerbaijani));
    }

    [Fact]
    public void UpdateShouldNormalizeProfileData()
    {
        UserProfile profile = UserProfile.Create(
            Guid.NewGuid(),
            "Ibadulla Huseynzade",
            PreferredLanguage.Azerbaijani);

        profile.Update(
            "  Ibadulla Huseynzade  ",
            "  RentoX developer  ",
            PreferredLanguage.English);

        Assert.Equal("Ibadulla Huseynzade", profile.FullName);
        Assert.Equal("RentoX developer", profile.Bio);
        Assert.Equal(
            PreferredLanguage.English,
            profile.PreferredLanguage);
    }
}