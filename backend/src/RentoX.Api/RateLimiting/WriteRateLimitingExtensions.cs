using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RentoX.Api.RateLimiting;

public static class WriteRateLimitingExtensions
{
    public static IServiceCollection AddRentoXWriteRateLimiting(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                HttpResponse response = context.HttpContext.Response;

                response.Headers.CacheControl = "no-store";

                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out TimeSpan retryAfter))
                {
                    response.Headers.RetryAfter =
                        Math.Max(
                            1,
                            Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Too many requests.",
                    detail: "Please wait before trying again.")
                    .ExecuteAsync(context.HttpContext);
            };

            AddUserPolicy(
                options,
                "conversation-create",
                20,
                TimeSpan.FromMinutes(10));

            AddUserPolicy(
                options,
                "message-send",
                60,
                TimeSpan.FromMinutes(1));

            AddUserPolicy(
                options,
                "image-upload",
                60,
                TimeSpan.FromMinutes(10));

            AddUserPolicy(
                options,
                "conversation-report",
                5,
                TimeSpan.FromHours(1));

            AddUserPolicy(
                options,
                "support-create",
                5,
                TimeSpan.FromHours(1));

            AddUserPolicy(
                options,
                "support-reply",
                20,
                TimeSpan.FromMinutes(10));
        });

        return services;
    }

    private static void AddUserPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        options.AddPolicy(
            policyName,
            context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetPartitionKey(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static string GetPartitionKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            string? identifier =
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(identifier, out Guid userId) &&
                userId != Guid.Empty)
            {
                return $"user:{userId:N}";
            }
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
