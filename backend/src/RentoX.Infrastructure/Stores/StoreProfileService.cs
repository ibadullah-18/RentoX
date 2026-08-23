using System.Text;
using Microsoft.EntityFrameworkCore;
using RentoX.Application.Stores;
using RentoX.Domain.Common.Exceptions;
using RentoX.Domain.Stores;
using RentoX.Infrastructure.Persistence;

namespace RentoX.Infrastructure.Stores;

public sealed class StoreProfileService(
    RentoXDbContext dbContext)
    : IStoreProfileService
{
    public async Task<StoreProfileResult> CreateAsync(
        CreateStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool ownerExists =
            await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    user =>
                        user.Id == command.OwnerId,
                    cancellationToken);

        if (!ownerExists)
        {
            throw new DomainException(
                "Store owner was not found.");
        }

        bool alreadyHasStore =
            await dbContext.StoreProfiles
                .AsNoTracking()
                .AnyAsync(
                    store =>
                        store.OwnerId ==
                            command.OwnerId,
                    cancellationToken);

        if (alreadyHasStore)
        {
            throw new DomainException(
                "The user already has a store.");
        }

        string slug =
            await CreateUniqueSlugAsync(
                command.Name,
                cancellationToken);

        StoreProfile store = StoreProfile.Create(
            command.OwnerId,
            command.Name,
            slug,
            command.Description,
            command.PhoneNumber,
            command.Email,
            command.Address,
            command.InstagramUrl,
            command.TiktokUrl,
            command.FacebookUrl,
            command.WebsiteUrl);

        dbContext.StoreProfiles.Add(store);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapResult(store);
    }

    public async Task<StoreProfileResult> UpdateAsync(
    UpdateStoreCommand command,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        StoreProfile store =
            await dbContext.StoreProfiles
                .SingleOrDefaultAsync(
                    item =>
                        item.OwnerId == command.OwnerId,
                    cancellationToken)
            ?? throw new DomainException(
                "Store was not found.");

        store.Update(
            command.Name,
            command.Description,
            command.PhoneNumber,
            command.Email,
            command.Address,
            command.InstagramUrl,
            command.TiktokUrl,
            command.FacebookUrl,
            command.WebsiteUrl);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return MapResult(store);
    }

    public async Task<StoreProfileResult?> GetMineAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        StoreProfile? store =
            await dbContext.StoreProfiles
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.OwnerId == ownerId,
                    cancellationToken);

        return store is null
            ? null
            : MapResult(store);
    }

    private async Task<string> CreateUniqueSlugAsync(
        string name,
        CancellationToken cancellationToken)
    {
        string baseSlug = GenerateSlug(name);

        bool exists =
            await dbContext.StoreProfiles
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(
                    store =>
                        store.Slug == baseSlug,
                    cancellationToken);

        if (!exists)
        {
            return baseSlug;
        }

        string suffix =
            Guid.NewGuid()
                .ToString("N")[..8];

        return $"{baseSlug}-{suffix}";
    }

    private static string GenerateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                "Store name is required.");
        }

        string source =
            value.Trim().ToLowerInvariant();

        StringBuilder builder = new();
        bool previousWasSeparator = false;

        foreach (char character in source)
        {
            char mapped = character switch
            {
                'ə' => 'e',
                'ı' => 'i',
                'ö' => 'o',
                'ü' => 'u',
                'ğ' => 'g',
                'ç' => 'c',
                'ş' => 's',
                _ => character
            };

            bool isLetterOrDigit =
                mapped is >= 'a' and <= 'z' ||
                mapped is >= '0' and <= '9';

            if (isLetterOrDigit)
            {
                builder.Append(mapped);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator &&
                     builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }

            if (builder.Length >= 100)
            {
                break;
            }
        }

        string slug =
            builder
                .ToString()
                .Trim('-');

        return string.IsNullOrWhiteSpace(slug)
            ? $"store-{Guid.NewGuid():N}"
            : slug;
    }

    private static StoreProfileResult MapResult(
        StoreProfile store)
    {
        return new StoreProfileResult(
            store.Id,
            store.OwnerId,
            store.Name,
            store.Slug,
            store.Description,
            store.PhoneNumber,
            store.Email,
            store.Address,
            store.LogoImageKey,
            store.CoverImageKey,
            store.InstagramUrl,
            store.TiktokUrl,
            store.FacebookUrl,
            store.WebsiteUrl,
            (int)store.Status,
            store.RejectionReason,
            store.CreatedAtUtc,
            store.UpdatedAtUtc);
    }
}