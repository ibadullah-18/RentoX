using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Locations;

/// <summary>A district of a <see cref="City"/>.</summary>
public sealed class District : Entity
{
    private District()
    {
    }

    private District(
        Guid id,
        Guid cityId,
        string nameAz,
        string nameRu,
        string nameEn,
        int displayOrder)
        : base(id)
    {
        if (cityId == Guid.Empty)
        {
            throw new DomainException("City id is required.");
        }

        CityId = cityId;
        NameAz = City.Require(nameAz);
        NameRu = City.Require(nameRu);
        NameEn = City.Require(nameEn);
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public Guid CityId { get; private set; }

    public string NameAz { get; private set; } = string.Empty;

    public string NameRu { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static District Create(
        Guid cityId,
        string nameAz,
        string nameRu,
        string nameEn,
        int displayOrder) =>
        new(Guid.NewGuid(), cityId, nameAz, nameRu, nameEn, displayOrder);

    public string NameFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => NameRu,
            PreferredLanguage.English => NameEn,
            _ => NameAz
        };

    public void Update(string nameAz, string nameRu, string nameEn, int displayOrder)
    {
        NameAz = City.Require(nameAz);
        NameRu = City.Require(nameRu);
        NameEn = City.Require(nameEn);
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
