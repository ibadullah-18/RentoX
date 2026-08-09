namespace RentoX.Application.Catalog.Fields;

public sealed record CategoryFieldOptionResult(
    Guid Id,
    string Value,
    int DisplayOrder,
    bool IsActive);