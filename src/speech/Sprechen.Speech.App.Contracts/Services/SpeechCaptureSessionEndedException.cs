namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Meldet ein reguläres Ende des Mikrofon-Chunksstroms. Wartende Aufnahmen
/// erhalten damit einen Fehler, auf den der Dialog mit Rückmeldung und
/// gegebenenfalls einem neuen Aufnahmeversuch reagieren kann.
/// </summary>
public sealed class SpeechCaptureSessionEndedException : InvalidOperationException
{
    public SpeechCaptureSessionEndedException()
        : base("The microphone capture session ended before the capture completed.")
    {
    }
}
