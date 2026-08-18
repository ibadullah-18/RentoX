using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Listings;

public sealed class ListingFieldValue : Entity
{
    private readonly List<ListingFieldSelection>
        _selections = [];

    private ListingFieldValue()
    {
    }

    private ListingFieldValue(
        Guid id,
        Guid listingId,
        Guid categoryFieldId)
        : base(id)
    {
        ListingId = ValidateId(
            listingId,
            "Listing");

        CategoryFieldId = ValidateId(
            categoryFieldId,
            "Category field");
    }

    public Guid ListingId { get; private set; }

    public Guid CategoryFieldId { get; private set; }

    public string? TextValue { get; private set; }

    public decimal? NumericValue { get; private set; }

    public bool? FlagValue { get; private set; }

    public DateOnly? CalendarValue { get; private set; }

    public string? CustomValue { get; private set; }

    public IReadOnlyCollection<ListingFieldSelection>
        Selections => _selections.AsReadOnly();

    public static ListingFieldValue Create(
        Guid listingId,
        Guid categoryFieldId)
    {
        return new ListingFieldValue(
            Guid.NewGuid(),
            listingId,
            categoryFieldId);
    }

    public void SetText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Text value is required.");
        }

        ClearScalarValues();
        TextValue = value.Trim();
    }

    public void SetNumber(decimal value)
    {
        ClearScalarValues();
        NumericValue = value;
    }

    public void SetFlag(bool value)
    {
        ClearScalarValues();
        FlagValue = value;
    }

    public void SetCalendarDate(DateOnly value)
    {
        ClearScalarValues();
        CalendarValue = value;
    }

    public void SetCustomValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Custom value is required.");
        }

        CustomValue = value.Trim();
    }

    public void AddSelection(Guid optionId)
    {
        if (optionId == Guid.Empty)
        {
            throw new DomainException(
                "Field option id is required.");
        }

        if (_selections.Any(selection =>
                selection.CategoryFieldOptionId ==
                optionId))
        {
            return;
        }

        _selections.Add(
            ListingFieldSelection.Create(
                Id,
                optionId));
    }

    private void ClearScalarValues()
    {
        TextValue = null;
        NumericValue = null;
        FlagValue = null;
        CalendarValue = null;
    }

    private static Guid ValidateId(
        Guid value,
        string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                $"{name} id is required.");
        }

        return value;
    }
}