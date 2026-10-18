namespace RentoX.Domain.Content;

/// <summary>
/// How a campaign (banner) looks and where it leads, apart from its picture and short texts: its own page on the
/// site (rentox.az/slug) with a long text and a button, whether the link opens in a new tab, where it is shown, and how
/// the text sits on the picture.
/// </summary>
/// <param name="PageSlug">The name of the campaign page after the slash ("birbank" for rentox.az/birbank); empty: no page.</param>
/// <param name="Platform">Where the banner is shown: 1 website, 2 apps, 3 both.</param>
/// <param name="TextAlign">0 left, 1 centre, 2 right.</param>
/// <param name="TextTone">0 light text, 1 dark text.</param>
public sealed record BannerCampaign(
    string? PageSlug = null,
    string? PageBodyAz = null,
    string? PageBodyRu = null,
    string? PageBodyEn = null,
    string? PageButtonLabelAz = null,
    string? PageButtonLabelRu = null,
    string? PageButtonLabelEn = null,
    string? PageButtonUrl = null,
    bool OpenInNewTab = false,
    int Platform = 3,
    bool ShowText = true,
    int TextAlign = 0,
    bool DarkOverlay = true,
    int TextTone = 0);
