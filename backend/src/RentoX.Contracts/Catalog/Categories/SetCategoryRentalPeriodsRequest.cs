namespace RentoX.Contracts.Catalog.Categories;

/// <summary>Periods: 1 hour, 2 day, 3 week, 4 month, 5 event, 6 negotiable.</summary>
public sealed record SetCategoryRentalPeriodsRequest(IReadOnlyList<int> Periods);
