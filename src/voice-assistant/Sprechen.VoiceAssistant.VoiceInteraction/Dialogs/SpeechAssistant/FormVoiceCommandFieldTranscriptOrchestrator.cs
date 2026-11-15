
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Verfügbarkeitsprüfung, Aufnahme, Befehle und besondere Eingabeabläufe. Abgelehnte Werteingabe erhält einen getrennten Navigations- beziehungsweise Befehlsweg.
/// </summary>
internal sealed class FormVoiceCommandFieldTranscriptOrchestrator
{
    private readonly FormVoiceCommandFieldTranscriptCapture _transcriptCapture;
    private readonly FormVoiceCommandTranscriptInterpretationService _transcriptInterpretationService;
    private readonly ParsedCommandBranch _parsedCommandBranch;
    private readonly FormVoiceCommandFieldEntryDependencies _fieldEntryDependencies;
    private readonly FormVoiceCommandDateComponentTranscriptBranch _dateComponentTranscriptBranch;
    private readonly FormVoiceCommandDigitsTranscriptBranch _digitsTranscriptBranch;
    private readonly FormVoiceCommandSpellingTranscriptBranch _spellingTranscriptBranch;
    private readonly FormVoiceCommandSetTranscriptBranch _setTranscriptBranch;
    private readonly FormVoiceCommandAmbiguityTranscriptBranch _ambiguityTranscriptBranch;

    public FormVoiceCommandFieldTranscriptOrchestrator(
        FormVoiceCommandFieldTranscriptCapture transcriptCapture,
        FormVoiceCommandTranscriptInterpretationService transcriptInterpretationService,
        ParsedCommandBranch parsedCommandBranch,
        FormVoiceCommandFieldEntryDependencies fieldEntryDependencies,
        FormVoiceCommandDateComponentTranscriptBranch dateComponentTranscriptBranch,
        FormVoiceCommandDigitsTranscriptBranch digitsTranscriptBranch,
        FormVoiceCommandSpellingTranscriptBranch spellingTranscriptBranch,
        FormVoiceCommandSetTranscriptBranch setTranscriptBranch,
        FormVoiceCommandAmbiguityTranscriptBranch ambiguityTranscriptBranch)
    {
        _transcriptCapture = transcriptCapture ?? throw new ArgumentNullException(nameof(transcriptCapture));
        _transcriptInterpretationService = transcriptInterpretationService ?? throw new ArgumentNullException(nameof(transcriptInterpretationService));
        _parsedCommandBranch = parsedCommandBranch ?? throw new ArgumentNullException(nameof(parsedCommandBranch));
        _fieldEntryDependencies = fieldEntryDependencies ?? throw new ArgumentNullException(nameof(fieldEntryDependencies));
        _dateComponentTranscriptBranch = dateComponentTranscriptBranch ?? throw new ArgumentNullException(nameof(dateComponentTranscriptBranch));
        _digitsTranscriptBranch = digitsTranscriptBranch ?? throw new ArgumentNullException(nameof(digitsTranscriptBranch));
        _spellingTranscriptBranch = spellingTranscriptBranch ?? throw new ArgumentNullException(nameof(spellingTranscriptBranch));
        _setTranscriptBranch = setTranscriptBranch ?? throw new ArgumentNullException(nameof(setTranscriptBranch));
        _ambiguityTranscriptBranch = ambiguityTranscriptBranch ?? throw new ArgumentNullException(nameof(ambiguityTranscriptBranch));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult> OrchestrateAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        bool announceReadOnlyFieldGroupContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        // Vor der Werteingabe die Feldverfügbarkeit prüfen. Ist die Eingabe
        // gesperrt, die Ablehnung behandeln und gegebenenfalls auf Befehle warten.
        var preCaptureDenial = await _fieldEntryDependencies.InputAvailabilityGuard.EvaluateAsync(
                turnContext,
                expectedInputMode,
                cancellationToken)
            .ConfigureAwait(false);
        if (preCaptureDenial is not null)
        {
            await _fieldEntryDependencies.ReadOnlyFieldSpeaker.AnnounceFieldGroupReadOnlyFieldsAsync(
                    turnContext, announceReadOnlyFieldGroupContent, cancellationToken)
                .ConfigureAwait(false);
            return await HandleDeniedTurnAsync(
                    turnContext,
                    preCaptureDenial,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        turnContext = turnContext with
        {
            FieldEntryPrompt = await _fieldEntryDependencies.ReadOnlyFieldSpeaker.ComposeFieldEntryPromptAsync(
                    turnContext, announceReadOnlyFieldGroupContent, cancellationToken)
                .ConfigureAwait(false)
        };
        var capture = await _transcriptCapture.CaptureAsync(turnContext, expectedInputMode, cancellationToken)
            .ConfigureAwait(false);
        if (capture.HasFinalResult)
        {
            return capture.FinalResult!;
        }

        turnContext.StateTracker.BeginInterpretation();

        var interpretation = _transcriptInterpretationService.Interpret(
            turnContext.Context,
            turnContext.Field,
            expectedInputMode,
            capture.RequiredTranscript);

        // Bei wörtlicher Erfassung das Transkript direkt als Eingabe weitergeben,
        // sofern die Befehlsprüfung keine Ausführung verlangt.
        if (interpretation.IsLiteralMode && !interpretation.ShouldExecuteParsedCommand)
        {
            return FormVoiceCommandFieldOrchestrationResult.ContinueWithTranscript(
                interpretation.InputTranscript,
                ambiguityTelemetry: null,
                capture.SttConfidencePercent);
        }

        var parsedCommandResult = await _parsedCommandBranch.TryHandleAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
        if (parsedCommandResult is not null)
        {
            return parsedCommandResult;
        }

        // Den Datumsbestandteilzweig vor dem Ziffern- und Buchstabierzweig prüfen.
        // Er entscheidet anhand von Moduskennzeichen und effektivem Werttyp.
        var dateComponentResult = await _dateComponentTranscriptBranch.TryHandleAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
        if (dateComponentResult is not null)
        {
            return dateComponentResult;
        }

        var digitsResult = await _digitsTranscriptBranch.TryHandleAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
        if (digitsResult is not null)
        {
            return digitsResult;
        }

        // Buchstabierte Eingaben über mehrere Aufnahmen sammeln, bevor der
        // zusammengesetzte Wert übernommen wird.
        var spellingResult = await _spellingTranscriptBranch.TryHandleAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
        if (spellingResult is not null)
        {
            return spellingResult;
        }

        // Die Mehrfachauswahl schrittweise sammeln und bearbeiten, bevor sie
        // als abgeschlossener Wert übernommen wird.
        var setResult = await _setTranscriptBranch.TryHandleAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
        if (setResult is not null)
        {
            return setResult;
        }

        return await _ambiguityTranscriptBranch.ResolveAsync(
                turnContext,
                interpretation,
                capture.SttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);
    }

    // Die erste Ablehnung für diesen Feldzustand vor der Ansage markieren.
    // Tastaturfelder warten auf einen Befehl. Bei Datenschutzablehnung zunächst
    // das nächste Feld versuchen. Scheitert der Wechsel, ebenfalls eine
    // Befehlsaufnahme öffnen. Bei erneutem Eintritt eine neutrale Warteansage verwenden.
    private async Task<FormVoiceCommandFieldOrchestrationResult> HandleDeniedTurnAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandPreCaptureDenial denial,
        CancellationToken cancellationToken)
    {
        var keyboardModeActive = string.Equals(
            denial.IssueCode,
            FormVoiceCommandFieldErrorCodes.KeyboardModeActive,
            StringComparison.Ordinal);
        var alreadyAnnounced = await turnContext.Interpreter
            .HasDeniedFieldTurnAnnouncementAsync()
            .ConfigureAwait(false);

        if (!alreadyAnnounced)
        {
            await _fieldEntryDependencies.InputAvailabilityGuard.TrackDenialAsync(turnContext, denial).ConfigureAwait(false);
            await turnContext.Interpreter.RecordDeniedFieldTurnAnnouncementAsync().ConfigureAwait(false);
        }

        string parkPrompt;
        if (keyboardModeActive && !alreadyAnnounced)
        {
            parkPrompt = denial.Prompt;
        }
        else
        {
            if (!alreadyAnnounced)
            {
                // Nach der Datenschutzansage zuerst den Fokuswechsel versuchen.
                // Nur ohne erfolgreichen Wechsel hier die Befehlsaufnahme öffnen.
                await _fieldEntryDependencies.InputAvailabilityGuard.AnnounceDenialAsync(
                        turnContext,
                        denial,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (await turnContext.Interpreter.TryMoveFocusNextAsync().ConfigureAwait(false))
                {
                    return FormVoiceCommandFieldOrchestrationResult.Completed(
                        FormVoiceCommandFieldHandlingResult.Continue());
                }
            }

            parkPrompt = FormInputFeedbackPromptBuilder.GetCommandParkWaitingPrompt(
                turnContext.Context.Contract,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings.GetPolitenessLevel());
        }

        var parkTranscript = await _fieldEntryDependencies.ConfirmationTranscriptCapture.CaptureAsync(
                turnContext.Context,
                turnContext.Session,
                turnContext.StateTracker,
                parkPrompt,
                cancellationToken,
                onStartListening: turnContext.StateTracker.BeginListening)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(parkTranscript))
        {
            turnContext.StateTracker.BeginInterpretation();
            var interpretation = _transcriptInterpretationService.Interpret(
                turnContext.Context,
                turnContext.Field,
                InputMode.Voice,
                parkTranscript);
            var commandResult = await _parsedCommandBranch.TryHandleAsync(
                    turnContext,
                    interpretation,
                    sttConfidencePercent: null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (commandResult is not null)
            {
                return commandResult;
            }
        }

        _ = await turnContext.Interpreter.TryMoveFocusNextAsync().ConfigureAwait(false);
        return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.Continue());
    }
}
