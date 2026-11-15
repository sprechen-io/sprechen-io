namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt Laden, Speichern, Zurücksetzen und sprachgeführtes Einrichten, damit die Oberfläche den Aktivierungsbestand über denselben Ablauf bearbeitet.
/// </summary>
public interface IWakeWordSettingsWorkflow
{
    ValueTask<WakeWordConfigurationState> LoadWakeWordConfigurationAsync(string? preferredIsoCode, CancellationToken cancellationToken);

    ValueTask<WakeWordConfigurationState> SaveWakeWordConfigurationAsync(
        string? editorText,
        string? preferredIsoCode,
        CancellationToken cancellationToken);

    ValueTask<WakeWordConfigurationState> ResetWakeWordConfigurationAsync(string? preferredIsoCode, CancellationToken cancellationToken);

    ValueTask<WakeWordConfigurationState> RunWakeWordSpeechSetupAsync(string? preferredIsoCode, CancellationToken cancellationToken);
}
