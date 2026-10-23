using System.Security.Claims;
using RentoX.Application.Authorization;

namespace RentoX.IntegrationTests.Authentication;

/// <summary>Which parts of the admin panel an Admin has, and the two that are only given on purpose.</summary>
public sealed class AdminSectionsTests
{
    private static ClaimsPrincipal Admin(params string[] sections) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, RoleNames.Admin), .. sections.Select(s => new Claim(AdminSections.ClaimType, s))],
            "test"));

    [Fact]
    public void AnAdminWithNoSectionChosenHasTheOrdinaryOnesButNotStaffOrBranding()
    {
        ClaimsPrincipal admin = Admin();

        Assert.True(AdminSections.Allows(admin, AdminSections.Listings));
        Assert.True(AdminSections.Allows(admin, AdminSections.Catalog));
        Assert.False(AdminSections.Allows(admin, AdminSections.Hr));
        Assert.False(AdminSections.Allows(admin, AdminSections.Branding));
    }

    [Fact]
    public void BrandingIsGivenOnPurposeAndOnlyThatIsOpened()
    {
        ClaimsPrincipal admin = Admin(AdminSections.Branding);

        Assert.True(AdminSections.Allows(admin, AdminSections.Branding));
        Assert.False(AdminSections.Allows(admin, AdminSections.Listings));
    }

    [Fact]
    public void TheOwnerHasEverything()
    {
        var owner = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, RoleNames.SuperAdmin)], "test"));

        Assert.All(AdminSections.All, section => Assert.True(AdminSections.Allows(owner, section)));
    }

    [Fact]
    public void NothingChosenIsRecordedAsTheOrdinarySectionsOnly()
    {
        int implied = AdminSections.ToMask([]);

        Assert.Equal(AdminSections.ToMask(AdminSections.Ordinary), implied);
        Assert.Equal(0, implied & (1 << Array.IndexOf(AdminSections.All, AdminSections.Hr)));
        Assert.Equal(0, implied & (1 << Array.IndexOf(AdminSections.All, AdminSections.Branding)));
    }
}
