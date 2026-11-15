namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Signalisiert ein fehlendes Sprachergebnis, damit Aufrufer darauf gesondert reagieren können.
/// </summary>
public sealed class NoSpeechDetectedException : InvalidOperationException
{
    public NoSpeechDetectedException()
        : base("No speech was detected.")
    {
    }
}


