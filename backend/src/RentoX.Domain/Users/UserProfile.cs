using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Users.Enums;
using RentoX.Domain.Users.Events;

namespace RentoX.Domain.Users;

public sealed class UserProfile : AuditableEntity
{
    private UserProfile()
    {
    }

    private UserProfile(
        Guid userId,
        string fullName,
        PreferredLanguage preferredLanguage)
        : base(userId)
    {
        FullName = ValidateFullName(fullName);
        PreferredLanguage = preferredLanguage;
        Status = UserStatus.Active;

        RaiseDomainEvent(
            new UserProfileCreatedDomainEvent(userId));
    }

    public string FullName { get; private set; } = string.Empty;

    public string? Bio { get; private set; }

    public string? ProfileImageKey { get; private set; }

    public PreferredLanguage PreferredLanguage { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    public static UserProfile Create(
        Guid userId,
        string fullName,
        PreferredLanguage preferredLanguage)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User ID cannot be empty.");
        }

        return new UserProfile(
            userId,
            fullName,
            preferredLanguage);
    }

    public void Update(
        string fullName,
        string? bio,
        PreferredLanguage preferredLanguage)
    {
        FullName = ValidateFullName(fullName);
        Bio = NormalizeOptionalText(bio, 500);
        PreferredLanguage = preferredLanguage;

        RaiseDomainEvent(
            new UserProfileUpdatedDomainEvent(Id));
    }

    public void SetProfileImage(string? profileImageKey)
    {
        ProfileImageKey =
            NormalizeOptionalText(profileImageKey, 500);
    }

    public void ChangeStatus(UserStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;

        RaiseDomainEvent(
            new UserStatusChangedDomainEvent(Id, status));
    }

    public void UpdateLastSeen(DateTimeOffset occurredAtUtc)
    {
        LastSeenAtUtc = occurredAtUtc;
    }

    private static string ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException(
                "Full name is required.");
        }

        string normalized = fullName.Trim();

        if (normalized.Length is < 2 or > 100)
        {
            throw new DomainException(
                "Full name must contain between 2 and 100 characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new DomainException(
                $"Value cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }
}