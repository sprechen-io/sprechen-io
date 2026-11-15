namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Kennzeichnet eine Aktivierung während der Aufwärmphase, damit sie nicht als betriebsbereite akustische Erkennung gilt.
/// </summary>
public sealed class AcousticWakeWordDetectorWarmupException : Exception
{
    public AcousticWakeWordDetectorWarmupException(string message)
        : base(message)
    {
    }
}
