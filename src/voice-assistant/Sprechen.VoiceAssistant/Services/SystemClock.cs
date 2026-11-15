namespace Sprechen.VoiceAssistant.Services;

/// <summary>
/// Stellt die aktuelle UTC-Zeit über die gemeinsame Uhrschnittstelle bereit.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}


