using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class FixedSystemClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
