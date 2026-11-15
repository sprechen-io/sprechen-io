
namespace Sprechen.Speech.TestDoubles;

public sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

