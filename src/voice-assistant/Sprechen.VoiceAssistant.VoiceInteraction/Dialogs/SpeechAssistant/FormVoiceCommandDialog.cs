using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Kontextaufbau und Sitzungslauf für einen Formularaufruf. Ohne geladenen Kontext erhält der Aufrufer ein Abbruchergebnis mit den Anfangswerten.
/// </summary>
internal sealed class FormVoiceCommandDialog(
    IFormVoiceCommandContextLoader contextLoader,
    IFormVoiceCommandSessionRunner sessionRunner) : IVoiceCommandDialog
{
    private readonly IFormVoiceCommandContextLoader _contextLoader = contextLoader ?? throw new ArgumentNullException(nameof(contextLoader));
    private readonly IFormVoiceCommandSessionRunner _sessionRunner = sessionRunner ?? throw new ArgumentNullException(nameof(sessionRunner));

    public async Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var valuesByVariableId = initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);
        var loadResult = await _contextLoader.LoadAsync(
                languageCode,
                applicationId,
                formId,
                applicationSession,
                valuesByVariableId,
                cancellationToken)
            .ConfigureAwait(false);

        if (loadResult.Context is null)
        {
            return loadResult.AbandonedResult
                ?? VoiceCommandDialogResult.Abandoned(
                    applicationId.Trim(),
                    string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(),
                    valuesByVariableId);
        }

        return await _sessionRunner.RunAsync(loadResult.Context, cancellationToken).ConfigureAwait(false);
    }
}


