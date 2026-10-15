namespace RentoX.Application.Locations;

public sealed record AdminDistrictItem(
    Guid Id,
    string NameAz,
    string NameRu,
    string NameEn,
    int DisplayOrder,
    bool IsActive,
    int ListingCount);

public sealed record AdminCityItem(
    Guid Id,
    string NameAz,
    string NameRu,
    string NameEn,
    int DisplayOrder,
    bool IsActive,
    int ListingCount,
    IReadOnlyList<AdminDistrictItem> Districts);

/// <summary>
/// Cities and districts as the staff manage them: inactive ones and every language included.
/// Nothing is deleted (listings point at them); a place is switched off instead.
/// </summary>
public interface IAdminLocationService
{
    Task<IReadOnlyList<AdminCityItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Guid> CreateCityAsync(
        string nameAz, string nameRu, string nameEn, int displayOrder,
        CancellationToken cancellationToken = default);

    Task UpdateCityAsync(
        Guid cityId, string nameAz, string nameRu, string nameEn, int displayOrder, bool isActive,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateDistrictAsync(
        Guid cityId, string nameAz, string nameRu, string nameEn, int displayOrder,
        CancellationToken cancellationToken = default);

    Task UpdateDistrictAsync(
        Guid districtId, string nameAz, string nameRu, string nameEn, int displayOrder, bool isActive,
        CancellationToken cancellationToken = default);
}
