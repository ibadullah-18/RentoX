using System.Globalization;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Moderation;

/// <summary>
/// A word or phrase that marks a listing for the moderators' attention. It is stored in lower case;
/// a trailing "*" matches every ending of the word ("dolandır*").
/// </summary>
public sealed class BannedWord : Entity
{
    public const int MinimumLength = 2;

    public const int MaximumLength = 50;

    private BannedWord()
    {
    }

    private BannedWord(Guid id, string word, Guid createdByUserId, DateTimeOffset createdAtUtc)
        : base(id)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The person who added the word is required.");
        }

        if (createdAtUtc == default || createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }

        Word = Normalize(word);
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public string Word { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static BannedWord Create(string word, Guid createdByUserId, DateTimeOffset createdAtUtc) =>
        new(Guid.NewGuid(), word, createdByUserId, createdAtUtc);

    /// <summary>Trimmed, single spaces, lower case (Azerbaijani rules: İ→i, I→ı).</summary>
    public static string Normalize(string? value)
    {
        string text = string.Join(' ', (value ?? string.Empty).Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        text = text.ToLower(Culture);

        string core = text.TrimEnd('*');
        if (core.Length < MinimumLength || text.Length > MaximumLength)
        {
            throw new DomainException(
                $"A banned word must be {MinimumLength} to {MaximumLength} characters.");
        }

        if (core.Contains('*', StringComparison.Ordinal) || text.Length - core.Length > 1)
        {
            throw new DomainException("Only one \"*\" is allowed, at the end of the word.");
        }

        return text;
    }

    internal static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("az");
}
