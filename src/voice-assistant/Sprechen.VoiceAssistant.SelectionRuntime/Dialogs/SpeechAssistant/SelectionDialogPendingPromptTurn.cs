using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt ausstehende Auswahlansagen aus und hält vorgezogene Aufnahmeergebnisse für die folgende
/// Phase bereit. Bestätigungsphase und Zuhörphase bestimmen dabei Aufnahmemodus und
/// Schlüsselbegriffe.
/// </summary>
internal sealed class SelectionDialogPendingPromptTurn<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionDefinition<TSelection, TResult> _definition;

    public SelectionDialogPendingPromptTurn(SelectionDialogExecutionDefinition<TSelection, TResult> definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public async Task RunAsync(
        SelectionDialogRunState<TSelection> state,
        CancellationToken cancellationToken)
    {
        var pendingPrompt = state.PendingPrompt;
        if (pendingPrompt is null)
        {
            return;
        }

        state.LastPrompt = pendingPrompt;
        if (state.Phase is SelectionDialogPhase.Listening or SelectionDialogPhase.Confirming)
        {
            // Die folgende Phase verwendet die hier aufgenommene Antwort. Deshalb wird
            // der Aufnahmemodus bereits bei der Ansage gewählt: kurze Antworten für
            // Bestätigungen, gewöhnlicher Aufnahmemodus für die Auswahl.
            state.PrefetchedCaptureResult = await _definition.Settings.Speech
                .SpeakAndListenAsync(
                    pendingPrompt,
                    _definition.Settings.IsoCode,
                    _definition.Settings.InteractionSettings,
                    _definition.Settings.NoInputTimeoutMs,
                    cancellationToken,
                    // Erkennungshinweise wie den Aufnahmemodus anhand der aktuellen Phase wählen,
                    // damit sie zur vorgezogenen Aufnahme und Bestätigungsstufe passen.
                    ResolveCaptureMode(state) == SpeechCaptureMode.ShortConfirmation
                        ? _definition.Settings.Keyterms?.ForConfirming(state.ConfirmationCaptureStage ?? SelectionConfirmationCaptureStage.Initial)
                        : _definition.Settings.Keyterms?.ForListening(),
                    ResolveCaptureMode(state))
                .ConfigureAwait(false);
        }
        else
        {
            await SpeakPromptAsync(pendingPrompt, cancellationToken).ConfigureAwait(false);
        }

        state.PendingPrompt = null;
    }

    public async Task ApplySpokenPromptAsync(
        SelectionDialogRunState<TSelection> state,
        SelectionDialogPrompt? spokenPrompt,
        CancellationToken cancellationToken)
    {
        if (spokenPrompt is null || !spokenPrompt.HasContent)
        {
            return;
        }

        if (state.Phase is SelectionDialogPhase.Listening or SelectionDialogPhase.Confirming)
        {
            state.PendingPrompt = spokenPrompt;
            return;
        }

        state.LastPrompt = spokenPrompt;
        await SpeakPromptAsync(spokenPrompt, cancellationToken).ConfigureAwait(false);
    }

    public Task SpeakPromptAsync(
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
    {
        return _definition.Settings.Speech.SpeakAsync(
            prompt,
            _definition.Settings.IsoCode,
            _definition.Settings.InteractionSettings,
            cancellationToken);
    }

    private static SpeechCaptureMode ResolveCaptureMode(SelectionDialogRunState<TSelection> state)
        => state.Phase == SelectionDialogPhase.Confirming
            ? SpeechCaptureMode.ShortConfirmation
            : SpeechCaptureMode.Listening;
}
