namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Trennt Ansagen und Antwortaufnahme vom Einrichtungsablauf,
/// damit dieser über Bestätigung und Speicherung statt über Audiotechnik entscheidet.
/// </summary>
internal interface IWakeWordSetupSpeechInteractor
{
    Task<bool?> PromptYesNoAsync(string prompt, string isoCode, int maxAttempts, CancellationToken cancellationToken);

    Task<SpeechCaptureResult> PromptSpeechToTextAsync(string prompt, string isoCode, CancellationToken cancellationToken);

    Task SpeakAsync(string text, string isoCode, CancellationToken cancellationToken);
}
