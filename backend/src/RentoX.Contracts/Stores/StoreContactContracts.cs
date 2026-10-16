namespace RentoX.Contracts.Stores;

/// <summary>One open day: Day 1 Monday to 7 Sunday; the hours as minutes after midnight.</summary>
public sealed record StoreHoursDayDto(int Day, int OpenMinute, int CloseMinute);

public sealed record UpdateStoreContactRequest(
    string PhoneNumber,
    string? Email,
    string? Address,
    string? InstagramUrl,
    string? TiktokUrl,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? WebsiteUrl,
    string? WhatsAppNumber,
    string? WhatsAppMessage,
    IReadOnlyList<StoreHoursDayDto>? WorkingHours);

/// <summary>Kind: view, call, whatsapp or message.</summary>
public sealed record StoreEventRequest(string Kind);

public sealed record StoreDayPointResponse(DateOnly Day, int Views, int Calls, int WhatsApp, int Messages);

public sealed record StoreStatsResponse(
    long Followers,
    long ActiveListings,
    long ListingViews,
    long ListingFavorites,
    long ProfileViews,
    long Calls,
    long WhatsApp,
    long Messages,
    long ProfileViewsLast7Days,
    long ProfileViewsLast30Days,
    long ContactsLast30Days,
    IReadOnlyList<StoreDayPointResponse> Last30Days);

/// <summary>The name and the description of a live store.</summary>
public sealed record UpdateStoreInfoRequest(string Name, string Description);
