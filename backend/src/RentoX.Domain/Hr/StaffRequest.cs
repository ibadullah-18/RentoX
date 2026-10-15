using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Hr;

public enum StaffRequestStatus
{
    /// <summary>Waiting for the person who watches the staff (or for the owner, when it was sent to the owner).</summary>
    Open = 1,

    /// <summary>The watcher passed it on to the owner.</summary>
    Forwarded = 2,

    Answered = 3,

    Rejected = 4
}

/// <summary>
/// A staff member writes to the management: to the person who looks after the staff, who answers or passes it to the
/// owner, or straight to the owner.
/// </summary>
public sealed class StaffRequest : Entity
{
    public const int MaximumSubjectLength = 160;

    public const int MaximumBodyLength = 2000;

    private StaffRequest()
    {
    }

    private StaffRequest(Guid id, Guid fromUserId, string subject, string body, bool toOwner, DateTimeOffset now)
        : base(id)
    {
        if (fromUserId == Guid.Empty)
        {
            throw new DomainException("The sender is required.");
        }

        string s = (subject ?? string.Empty).Trim();
        string b = (body ?? string.Empty).Trim();
        if (s.Length is 0 or > MaximumSubjectLength)
        {
            throw new DomainException($"The subject is required (up to {MaximumSubjectLength} characters).");
        }

        if (b.Length is 0 or > MaximumBodyLength)
        {
            throw new DomainException($"The message is required (up to {MaximumBodyLength} characters).");
        }

        StaffProfile.RequireUtc(now);
        FromUserId = fromUserId;
        Subject = s;
        Body = b;
        ToOwner = toOwner;
        Status = StaffRequestStatus.Open;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid FromUserId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    /// <summary>True when it goes to the owner (sent so, or passed on).</summary>
    public bool ToOwner { get; private set; }

    public StaffRequestStatus Status { get; private set; }

    public string? Answer { get; private set; }

    public Guid? HandledByUserId { get; private set; }

    public DateTimeOffset? HandledAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static StaffRequest Create(Guid fromUserId, string subject, string body, bool toOwner, DateTimeOffset now) =>
        new(Guid.NewGuid(), fromUserId, subject, body, toOwner, now);

    /// <summary>The watcher passes it to the owner, with a note of their own.</summary>
    public void Forward(Guid byUserId, string? note, DateTimeOffset now)
    {
        RequireOpen();
        RequireUser(byUserId);
        ToOwner = true;
        Status = StaffRequestStatus.Forwarded;
        Answer = Clean(note);
        HandledByUserId = byUserId;
        UpdatedAtUtc = now;
    }

    public void Resolve(bool accepted, string? answer, Guid byUserId, DateTimeOffset now)
    {
        if (Status is StaffRequestStatus.Answered or StaffRequestStatus.Rejected)
        {
            throw new DomainException("This request has already been answered.");
        }

        RequireUser(byUserId);
        StaffProfile.RequireUtc(now);

        Status = accepted ? StaffRequestStatus.Answered : StaffRequestStatus.Rejected;
        Answer = Clean(answer);
        HandledByUserId = byUserId;
        HandledAtUtc = now;
        UpdatedAtUtc = now;
    }

    private void RequireOpen()
    {
        if (Status != StaffRequestStatus.Open)
        {
            throw new DomainException("Only a request that is still open can be passed on.");
        }
    }

    private static void RequireUser(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("The person who handled the request is required.");
        }
    }

    private static string? Clean(string? text)
    {
        string? cleaned = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (cleaned is { Length: > MaximumBodyLength })
        {
            throw new DomainException($"The text cannot exceed {MaximumBodyLength} characters.");
        }

        return cleaned;
    }
}
