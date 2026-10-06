using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RentoX.Infrastructure.Authentication;

namespace RentoX.Api.Authentication;

public static class JwtBearerConfiguration
{
    private static readonly string[] AllowedAlgorithms =
        [SecurityAlgorithms.HmacSha256];

    public static void Configure(
        JwtBearerOptions options,
        JwtOptions jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(jwtOptions);

        options.MapInboundClaims = true;
        options.IncludeErrorDetails = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                RequireAudience = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                RequireSignedTokens = true,
                ValidAlgorithms = AllowedAlgorithms,

                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(30),

                RoleClaimType = ClaimTypes.Role
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                bool isHubRequest =
                    context.Request.Path.StartsWithSegments(
                        "/hubs/conversations") ||
                    context.Request.Path.StartsWithSegments(
                        "/hubs/notifications");

                if (isHubRequest &&
                    !context.Request.Headers.ContainsKey("Authorization"))
                {
                    string accessToken =
                        context.Request.Query["access_token"].ToString();

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                if (!AuthenticatedUserId.Resolve(context.Principal).HasValue)
                {
                    context.Fail("A valid user identity is required.");
                }

                return Task.CompletedTask;
            }
        };
    }
}
