namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Meldet den Beginn einer Sprachaufnahme an die Ausgabesteuerung,
/// damit der Aufnahmeablauf Dazwischensprechen ohne eigene Wiedergabelogik auslösen kann.
/// </summary>
public interface IUserSpeechBargeInHandler
{
    ValueTask OnSpeechCaptureStartingAsync(CancellationToken cancellationToken);
}




