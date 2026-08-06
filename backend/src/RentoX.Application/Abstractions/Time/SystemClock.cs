using RentoX.Application.Abstractions.Time;

namespace RentoX.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}