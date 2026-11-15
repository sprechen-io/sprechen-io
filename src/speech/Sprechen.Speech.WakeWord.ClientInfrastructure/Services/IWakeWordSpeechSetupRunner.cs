namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Stellt den gesprochenen Einrichtungsablauf als eigenen Zugang bereit,
/// damit Einstellungen ihn ohne Kenntnis seiner einzelnen Dialogschritte starten können.
/// </summary>
internal interface IWakeWordSpeechSetupRunner
{
    Task EnsureWakeWordsConfiguredAsync(string isoCode, CancellationToken cancellationToken);
}
