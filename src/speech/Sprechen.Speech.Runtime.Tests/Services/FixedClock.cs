
namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

