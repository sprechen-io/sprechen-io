namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den Abschlussdialog mit einer begrenzten Auswahl modellierter Befehle für
/// Prüfen, Ändern, Absenden und Abbrechen. Nicht aufgelöste oder nicht zugelassene
/// Befehle führen erneut zum Angebot. Ein geändertes nichtleeres Fokusziel gibt die
/// Steuerung an den Feldablauf zurück.
/// </summary>
internal sealed class FormClosingReviewTurnFlow
{
    // Nur die hier aufgezählten Befehle im Abschlussdialog weiterleiten.
    // Weiter bleibt ausgeschlossen: ohne Folgefeld sendet der Navigationshandler das Formular.
    // Zurück darf weitergeleitet werden. Ein Fokuswechsel gibt die Steuerung an den Feldablauf zurück.
    private static readonly HashSet<string> ClosingTurnRoutableCommands = new(StringComparer.Ordinal)
    {
        GlobalCommandIds.Summary,
        GlobalCommandIds.Status,
        GlobalCommandIds.FormStatus,
        GlobalCommandIds.FieldList,
        GlobalCommandIds.FieldGroupList,
        GlobalCommandIds.CommandList,
        GlobalCommandIds.Repeat,
        GlobalCommandIds.Help,
        GlobalCommandIds.JumpTo,
        GlobalCommandIds.Back,
        GlobalCommandIds.Submit,
        GlobalCommandIds.Cancel
    };

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandGlobalCommandExecutionService _commandExecutionService;

    public FormClosingReviewTurnFlow(
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandGlobalCommandExecutionService commandExecutionService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _commandExecutionService = commandExecutionService ?? throw new ArgumentNullException(nameof(commandExecutionService));
    }

    public Task<FormClosingReviewTurnResult> RunAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        CancellationToken cancellationToken)
        => RunAsync(
            context,
            interpreter,
            session,
            stateTracker,
            committedValueReadbackPrompt: null,
            cancellationToken);

    public async Task<FormClosingReviewTurnResult> RunAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string? committedValueReadbackPrompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);

        stateTracker.BeginClosingReview();

        var isoCode = context.IsoCode;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var closingPrompt = FormDialogFlowPromptTextResolver.GetFormClosingReviewPrompt(isoCode, politenessLevel);
        var firstOfferPrompt = string.IsNullOrWhiteSpace(committedValueReadbackPrompt)
            ? closingPrompt
            : string.Concat(committedValueReadbackPrompt, " ", closingPrompt);

        // Den Ausgangsfokus merken. Nach einem weitergeleiteten Befehl nur bei einem
        // anderen nichtleeren Fokusziel zum Feldablauf zurückkehren.
        var restingFocusFieldId = await TryGetFocusedFieldIdAsync(interpreter).ConfigureAwait(false);
        var firstOffer = true;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prompt = firstOffer ? firstOfferPrompt : closingPrompt;
            firstOffer = false;

            var transcript = await CaptureAsync(context, session, stateTracker, prompt, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                continue;
            }

            if (!FormGlobalCommandParser.TryParse(
                    context.Contract,
                    transcript,
                    isoCode,
                    politenessLevel,
                    out var commandId,
                    out var argument))
            {
                continue;
            }

            // Einen nicht für diesen Dialog zugelassenen Befehl nicht weiterleiten,
            // sondern das Abschlussangebot erneut anfordern. Die Erkennung trotzdem
            // protokollieren, damit dieser Zweig nicht wie Stille oder ein unerkannter
            // Satz aussieht.
            if (!ClosingTurnRoutableCommands.Contains(commandId.Trim()))
            {
                _commandExecutionService.TrackRecognizedWithoutRouting(session, commandId);
                continue;
            }

            var commandResult = await _commandExecutionService.RouteAsync(
                    context,
                    interpreter,
                    session,
                    stateTracker,
                    commandId,
                    argument,
                    cancellationToken)
                .ConfigureAwait(false);

            if (commandResult is not null)
            {
                return FormClosingReviewTurnResult.AsTerminal(
                    FormVoiceCommandFieldHandlingResult.FromDialogResult(commandResult));
            }

            // Bei anderem nichtleerem Fokusziel zum Feldablauf zurückkehren.
            // Bei unverändertem oder leerem Fokus das Abschlussangebot wiederholen.
            var focusFieldId = await TryGetFocusedFieldIdAsync(interpreter).ConfigureAwait(false);
            if (focusFieldId is not null
                && !string.Equals(focusFieldId, restingFocusFieldId, StringComparison.Ordinal))
            {
                stateTracker.RecordClosingReviewChangeRequested();
                return FormClosingReviewTurnResult.ReturnToFieldLoop();
            }
        }
    }

    private async Task<string?> CaptureAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var captureResult = await _speechService.CaptureTranscriptAsync(
                    prompt,
                    context.IsoCode,
                    context.CaptureSettings,
                    onStartListening: stateTracker.BeginListening,
                    new SpeechCaptureTelemetryContext(session),
                    RecognitionProfile.Conversational,
                    keyterms: null,
                    cancellationToken)
                .ConfigureAwait(false);

            return captureResult.Transcript;
        }
        catch (NoSpeechDetectedException)
        {
            return null;
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Einen als vorübergehend eingestuften Aufnahmefehler als leeren Ausgang
            // zurückgeben, damit das Abschlussangebot erneut angefordert wird.
            return null;
        }
    }

    private static Task<string?> TryGetFocusedFieldIdAsync(IFormDialogRuntime interpreter)
        => interpreter.GetFocusedFieldIdAsync();

}
