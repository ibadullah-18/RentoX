using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Finance;

/// <summary>Who signs the monthly report: name, tax number (VÖEN) and how to reach them. One row for the whole site.</summary>
public sealed class FinanceProfile : Entity
{
    public const int MaximumLength = 200;

    /// <summary>The single row always has this id.</summary>
    public static readonly Guid SingleId = new("5f1a8c20-7a43-4c77-9d0e-3c6b9a4f1e01");

    private FinanceProfile()
    {
    }

    private FinanceProfile(Guid id)
        : base(id)
    {
    }

    public string FullName { get; private set; } = string.Empty;

    public string? Voen { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static FinanceProfile CreateEmpty() => new(SingleId);

    public void Update(string fullName, string? voen, string? phone, string? email, string? address, Guid byUserId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > MaximumLength)
        {
            throw new DomainException("The full name is required.");
        }

        if (byUserId == Guid.Empty)
        {
            throw new DomainException("The person who changed the details is required.");
        }

        string? Clean(string? value, string field)
        {
            string? text = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (text is { Length: > MaximumLength })
            {
                throw new DomainException($"{field} is too long.");
            }

            return text;
        }

        FullName = fullName.Trim();
        Voen = Clean(voen, "The tax number");
        Phone = Clean(phone, "The phone");
        Email = Clean(email, "The e-mail");
        Address = Clean(address, "The address");
        UpdatedByUserId = byUserId;
        UpdatedAtUtc = now;
    }
}
