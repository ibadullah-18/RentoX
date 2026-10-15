using System.Text.RegularExpressions;
using RentoX.Domain.Common;
using RentoX.Domain.Common.Exceptions;

namespace RentoX.Domain.Content;

/// <summary>
/// Who runs the site and how to reach them: the operator's details and the contact channels shown in
/// the footer and the app. Every field is optional; a channel that is empty is not shown anywhere.
/// </summary>
public sealed class SiteInfo : Entity
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static readonly Regex PhonePattern = new(
        @"^\+?[0-9()\-\s]{3,25}$", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private static readonly Regex HandlePattern = new(
        @"^[A-Za-z0-9._-]{2,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private SiteInfo()
    {
    }

    private SiteInfo(Guid id)
        : base(id)
    {
    }

    public string? OperatorName { get; private set; }

    public string? Voen { get; private set; }

    public string? Address { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>The short number people call for help ("qaynar xətt").</summary>
    public string? Hotline { get; private set; }

    /// <summary>The WhatsApp number, with the country code, digits only after a leading "+".</summary>
    public string? WhatsApp { get; private set; }

    public string? Instagram { get; private set; }

    public string? TikTok { get; private set; }

    public string? Facebook { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static SiteInfo CreateEmpty() => new(Guid.NewGuid());

    public void Update(
        string? operatorName, string? voen, string? address,
        string? email, string? phone, string? hotline, string? whatsApp,
        string? instagram, string? tikTok, string? facebook,
        DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (nowUtc == default || nowUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException("The time must be a UTC time.");
        }

        if (updatedByUserId == Guid.Empty)
        {
            throw new DomainException("The person who edited the details is invalid.");
        }

        // Check every value first; only then change anything (a refused edit must change nothing).
        string? newOperatorName = Text(operatorName, 150, "Operator name");
        string? newVoen = Text(voen, 30, "VÖEN");
        string? newAddress = Text(address, 300, "Address");
        string? newEmail = Email_(email);
        string? newPhone = PhoneNumber(phone, "Phone");
        string? newHotline = PhoneNumber(hotline, "Hotline");
        string? newWhatsApp = WhatsAppNumber(whatsApp);
        string? newInstagram = Social(instagram, "Instagram", ["instagram.com"], "https://www.instagram.com/");
        string? newTikTok = Social(tikTok, "TikTok", ["tiktok.com"], "https://www.tiktok.com/@");
        string? newFacebook = Social(facebook, "Facebook", ["facebook.com", "fb.com"], "https://www.facebook.com/");

        OperatorName = newOperatorName;
        Voen = newVoen;
        Address = newAddress;
        Email = newEmail;
        Phone = newPhone;
        Hotline = newHotline;
        WhatsApp = newWhatsApp;
        Instagram = newInstagram;
        TikTok = newTikTok;
        Facebook = newFacebook;
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = updatedByUserId;
    }

    private static string? Text(string? value, int maximum, string field)
    {
        string text = (value ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length > maximum)
        {
            throw new DomainException($"{field} cannot exceed {maximum} characters.");
        }

        return text;
    }

    private static string? Email_(string? value)
    {
        string? text = Text(value, 150, "E-mail");
        if (text is null)
        {
            return null;
        }

        if (!EmailPattern.IsMatch(text))
        {
            throw new DomainException("E-mail address is not valid.");
        }

        return text;
    }

    private static string? PhoneNumber(string? value, string field)
    {
        string? text = Text(value, 25, field);
        if (text is null)
        {
            return null;
        }

        if (!PhonePattern.IsMatch(text))
        {
            throw new DomainException($"{field} number is not valid.");
        }

        return text;
    }

    private static string? WhatsAppNumber(string? value)
    {
        string? text = Text(value, 30, "WhatsApp");
        if (text is null)
        {
            return null;
        }

        string digits = new(text.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length is < 8 or > 15 || text.Any(c => !(char.IsAsciiDigit(c) || c is '+' or ' ' or '-' or '(' or ')')))
        {
            throw new DomainException("WhatsApp number is not valid. Write it with the country code, e.g. +994501234567.");
        }

        return "+" + digits;
    }

    /// <summary>
    /// A link to a profile: either a full address on the network's own site, or just the profile name.
    /// Anything else (other sites, other schemes, user info) is refused so the footer can never lead somewhere unexpected.
    /// </summary>
    private static string? Social(string? value, string field, string[] hosts, string handleBase)
    {
        string? text = Text(value, 300, field);
        if (text is null)
        {
            return null;
        }

        if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !hosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase) ||
                                uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DomainException($"{field} link must be an address on {hosts[0]}.");
            }

            return "https://" + uri.Host + uri.PathAndQuery;
        }

        string handle = text.TrimStart('@');
        if (!HandlePattern.IsMatch(handle))
        {
            throw new DomainException($"{field} must be a profile name or a link.");
        }

        return handleBase + handle;
    }
}
