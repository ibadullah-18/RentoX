namespace RentoX.Contracts.Catalog.Fields;

public sealed record CategoryFieldOptionResponse(
    Guid Id,
    string Value,
    int DisplayOrder,
    bool IsActive);