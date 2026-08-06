using Microsoft.AspNetCore.Identity;

namespace RentoX.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public DateTimeOffset RegisteredAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }
}