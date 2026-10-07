using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.UnitTests.Users;

public sealed class UserProfileValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void CreateRejectsInvalidLanguage(int language)
    {
        Assert.Throws<DomainException>(() =>
            UserProfile.Create(
                Guid.NewGuid(),
                "Original Name",
                (PreferredLanguage)language));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void InvalidLanguageLeavesProfileUnchanged(int language)
    {
        UserProfile profile = CreateProfile();

        Assert.Throws<DomainException>(() =>
            profile.Update(
                "Changed Name",
                "Changed bio",
                (PreferredLanguage)language));

        AssertOriginalValues(profile);
    }

    [Fact]
    public void InvalidBioLeavesProfileUnchanged()
    {
        UserProfile profile = CreateProfile();

        Assert.Throws<DomainException>(() =>
            profile.Update(
                "Changed Name",
                new string('x', 501),
                PreferredLanguage.English));

        AssertOriginalValues(profile);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("X")]
    public void InvalidNameLeavesProfileUnchanged(string? name)
    {
        UserProfile profile = CreateProfile();

        Assert.Throws<DomainException>(() =>
            profile.Update(
                name!,
                "Changed bio",
                PreferredLanguage.English));

        AssertOriginalValues(profile);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void InvalidStatusLeavesProfileUnchanged(int status)
    {
        UserProfile profile = CreateProfile();

        Assert.Throws<DomainException>(() =>
            profile.ChangeStatus((UserStatus)status));

        Assert.Equal(UserStatus.Active, profile.Status);
        AssertOriginalValues(profile);
    }

    [Fact]
    public void ValidUpdateNormalizesTextAndPreservesOtherFields()
    {
        UserProfile profile = CreateProfile();
        Guid originalId = profile.Id;

        profile.SetProfileImage("profiles/avatar.webp");

        profile.Update(
            "  Updated Name  ",
            "  Updated bio  ",
            PreferredLanguage.English);

        Assert.Equal("Updated Name", profile.FullName);
        Assert.Equal("Updated bio", profile.Bio);
        Assert.Equal(
            PreferredLanguage.English,
            profile.PreferredLanguage);
        Assert.Equal(originalId, profile.Id);
        Assert.Equal(UserStatus.Active, profile.Status);
        Assert.Equal(
            "profiles/avatar.webp",
            profile.ProfileImageKey);
    }

    private static UserProfile CreateProfile()
    {
        UserProfile profile = UserProfile.Create(
            Guid.NewGuid(),
            "Original Name",
            PreferredLanguage.Azerbaijani);

        profile.Update(
            "Original Name",
            "Original bio",
            PreferredLanguage.Azerbaijani);

        return profile;
    }

    private static void AssertOriginalValues(UserProfile profile)
    {
        Assert.Equal("Original Name", profile.FullName);
        Assert.Equal("Original bio", profile.Bio);
        Assert.Equal(
            PreferredLanguage.Azerbaijani,
            profile.PreferredLanguage);
    }
}
