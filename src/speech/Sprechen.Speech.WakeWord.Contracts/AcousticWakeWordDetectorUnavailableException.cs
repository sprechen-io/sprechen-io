namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Kennzeichnet einen nicht verfügbaren akustischen Detektor, damit die Laufzeit diesen Ersatzgrund von Erkennungsfehlern trennt.
/// </summary>
public sealed class AcousticWakeWordDetectorUnavailableException : Exception
{
    public AcousticWakeWordDetectorUnavailableException(string message)
        : base(message)
    {
    }

    public AcousticWakeWordDetectorUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
