using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentoX.Application.Files;

namespace RentoX.Infrastructure.Files;

public static class FileStorageRegistration
{
    /// <summary>
    /// Where the files live, by the setting <c>Storage:Provider</c>: "Disk" (the default: a folder, <c>Storage:RootPath</c>)
    /// or "R2" (Cloudflare R2, <c>Storage:R2:*</c>). The API and the worker must use the same one.
    /// </summary>
    public static IServiceCollection AddRentoXFileStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IFileStorage>(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            string kind = (configuration["Storage:Provider"] ?? "Disk").Trim();

            if (kind.Equals("R2", StringComparison.OrdinalIgnoreCase))
            {
                return new R2FileStorage(configuration);
            }

            if (kind.Equals("Disk", StringComparison.OrdinalIgnoreCase))
            {
                return ActivatorUtilities.CreateInstance<LocalFileStorage>(provider);
            }

            throw new InvalidOperationException(
                $"Storage:Provider must be Disk or R2, not '{kind}'.");
        });

        return services;
    }
}
