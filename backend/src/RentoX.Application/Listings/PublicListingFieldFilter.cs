namespace RentoX.Application.Listings;

/// <summary>
/// One condition on a dynamic category field. Set the members that fit the
/// field's type: <see cref="OptionIds"/> (select: any of them),
/// <see cref="Min"/>/<see cref="Max"/> (number), <see cref="Flag"/>
/// (yes/no) or <see cref="From"/>/<see cref="To"/> (date). Conditions on
/// different fields must all hold.
/// </summary>
public sealed record PublicListingFieldFilter(
    Guid FieldId,
    Guid[]? OptionIds = null,
    decimal? Min = null,
    decimal? Max = null,
    bool? Flag = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? CustomValue = null);
