using Microsoft.EntityFrameworkCore;
using RentoX.Application.Locations;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Infrastructure.Locations;
using RentoX.Infrastructure.Persistence;
using RentoX.IntegrationTests.Auditing;

namespace RentoX.IntegrationTests.Moderation;

/// <summary>Cities and districts managed by the staff: add, rename, switch off, no duplicates.</summary>
public sealed class AdminLocationTests(AuditPostgresFixture database)
    : IClassFixture<AuditPostgresFixture>
{
    [Fact]
    public async Task ACityAndItsDistrictCanBeAddedRenamedAndSwitchedOff()
    {
        string tag = Guid.NewGuid().ToString("N")[..6];
        Guid city = await Service().CreateCityAsync($"Test şəhər {tag}", $"Тест {tag}", $"Test city {tag}", 50);
        Guid district = await Service().CreateDistrictAsync(city, $"Rayon {tag}", $"Район {tag}", $"District {tag}", 1);

        AdminCityItem created = (await Service().GetAllAsync()).Single(c => c.Id == city);
        Assert.True(created.IsActive);
        Assert.Equal(district, created.Districts.Single().Id);

        await Service().UpdateCityAsync(city, $"Yeni ad {tag}", $"Новое {tag}", $"New {tag}", 60, isActive: false);
        await Service().UpdateDistrictAsync(district, $"Rayon {tag}", $"Район {tag}", $"District {tag}", 2, isActive: false);

        AdminCityItem changed = (await Service().GetAllAsync()).Single(c => c.Id == city);
        Assert.Equal($"Yeni ad {tag}", changed.NameAz);
        Assert.Equal(60, changed.DisplayOrder);
        Assert.False(changed.IsActive);
        Assert.False(changed.Districts.Single().IsActive);

        // What the public sees: a switched-off city is gone from the list.
        await using RentoXDbContext db = database.CreateContext();
        var publicCities = await new LocationQueryService(db).GetCitiesAsync(PreferredLanguage.Azerbaijani);
        Assert.DoesNotContain(publicCities, c => c.Id == city);
    }

    [Fact]
    public async Task ASecondCityWithTheSameNameIsRefused()
    {
        string name = "Dublikat " + Guid.NewGuid().ToString("N")[..6];
        Guid first = await Service().CreateCityAsync(name, "Дубль", "Duplicate", 1);

        await Assert.ThrowsAsync<DomainException>(() => Service().CreateCityAsync(name, "Дубль 2", "Duplicate 2", 2));

        // Renaming another city to this name is refused too.
        Guid other = await Service().CreateCityAsync(name + " 2", "Другой", "Other", 3);
        await Assert.ThrowsAsync<DomainException>(() => Service().UpdateCityAsync(other, name, "Другой", "Other", 3, true));
        Assert.Equal(name + " 2", (await Service().GetAllAsync()).Single(c => c.Id == other).NameAz);
        Assert.NotEqual(first, other);
    }

    [Fact]
    public async Task EmptyNamesAndUnknownCitiesAreRefused()
    {
        await Assert.ThrowsAsync<DomainException>(() => Service().CreateCityAsync(" ", "x", "x", 1));
        await Assert.ThrowsAsync<DomainException>(() => Service().CreateDistrictAsync(Guid.NewGuid(), "a", "b", "c", 1));
        await Assert.ThrowsAsync<DomainException>(() => Service().UpdateCityAsync(Guid.NewGuid(), "a", "b", "c", 1, true));
        await Assert.ThrowsAsync<DomainException>(() => Service().CreateCityAsync("Sıra", "x", "x", 20000));
    }

    private AdminLocationService Service() => new(database.CreateContext());
}
