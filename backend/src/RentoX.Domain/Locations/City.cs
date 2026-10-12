using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;

namespace RentoX.Domain.Locations;

/// <summary>A city (or region centre) a listing can be placed in.</summary>
public sealed class City : Entity
{
    private City()
    {
    }

    private City(
        Guid id,
        string nameAz,
        string nameRu,
        string nameEn,
        int displayOrder)
        : base(id)
    {
        NameAz = Require(nameAz);
        NameRu = Require(nameRu);
        NameEn = Require(nameEn);
        DisplayOrder = displayOrder;
        IsActive = true;
    }

    public string NameAz { get; private set; } = string.Empty;

    public string NameRu { get; private set; } = string.Empty;

    public string NameEn { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static City Create(
        string nameAz,
        string nameRu,
        string nameEn,
        int displayOrder) =>
        new(Guid.NewGuid(), nameAz, nameRu, nameEn, displayOrder);

    public string NameFor(PreferredLanguage language) =>
        language switch
        {
            PreferredLanguage.Russian => NameRu,
            PreferredLanguage.English => NameEn,
            _ => NameAz
        };

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    internal static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Location name is required.");
        }

        string trimmed = value.Trim();

        if (trimmed.Length > 100)
        {
            throw new DomainException(
                "Location name cannot exceed 100 characters.");
        }

        return trimmed;
    }
}
