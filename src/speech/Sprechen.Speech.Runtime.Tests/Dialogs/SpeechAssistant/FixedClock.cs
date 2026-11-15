
namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

