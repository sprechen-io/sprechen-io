using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Speichert die gewählte Sprache über den gemeinsamen Ablauf und prüft den zurückgegebenen
/// Sprachcode. Für den Neustart wird damit das Ergebnis des Speicherns verwendet.
/// </summary>
internal sealed class FormsCatalogLanguageChangePersistence
{
    private readonly FormsCatalogLanguageChangeWorkflow _languageChangeWorkflow;
    private readonly ISpeechAssistantTrace _speechAssistantTrace;

    public FormsCatalogLanguageChangePersistence(
        FormsCatalogLanguageChangeWorkflow languageChangeWorkflow,
        ISpeechAssistantTrace speechAssistantTrace)
    {
        _languageChangeWorkflow = languageChangeWorkflow ?? throw new ArgumentNullException(nameof(languageChangeWorkflow));
        _speechAssistantTrace = speechAssistantTrace ?? throw new ArgumentNullException(nameof(speechAssistantTrace));
    }

    public async Task<string> PersistAsync(
        string? correlationId,
        string targetIsoCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetIsoCode);

        var settings = await _languageChangeWorkflow
            .SaveSelectedLanguageAsync(targetIsoCode, cancellationToken)
            .ConfigureAwait(false);
        var persistedIsoCode = IsoCode.TryNormalizeIso6391(settings.SelectedIsoCode)
            ?? throw new InvalidOperationException("Expected language settings workflow to persist a normalized ISO 639-1 code.");

        await _speechAssistantTrace
            .WriteIfConfiguredAsync(
                string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId.Trim(),
                SpeechAssistantTraceScope.FormsCatalogDialog,
                SpeechAssistantTraceLevel.Information,
                $"Application catalog dialog persisted a pre-session language change to isoCode={persistedIsoCode}.",
                cancellationToken)
            .ConfigureAwait(false);

        return persistedIsoCode;
    }
}
