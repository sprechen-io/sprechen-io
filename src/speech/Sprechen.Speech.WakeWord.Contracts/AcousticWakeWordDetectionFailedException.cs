namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Kennzeichnet einen gescheiterten akustischen Erkennungsversuch, damit die Laufzeit den passenden lexikalischen Ersatzpfad wählen kann.
/// </summary>
public sealed class AcousticWakeWordDetectionFailedException : Exception
{
    public AcousticWakeWordDetectionFailedException(string message)
        : base(message)
    {
    }

    public AcousticWakeWordDetectionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
