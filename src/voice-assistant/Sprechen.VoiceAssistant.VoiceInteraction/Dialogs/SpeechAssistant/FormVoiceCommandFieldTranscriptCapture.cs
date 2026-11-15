namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die Feldaufnahme mit passenden Erkennungseinstellungen an und übergibt fehlende oder fehlerhafte Ergebnisse mit getrennten Kennungen an die Fehlerbehandlung.
/// </summary>
internal sealed class FormVoiceCommandFieldTranscriptCapture
{
    private const int DefaultOptionListingInitialSilenceTimeoutMs = 18000;
    private const int MinimumOptionListingInitialSilenceTimeoutMs = 1000;
    private const int MaximumOptionListingInitialSilenceTimeoutMs = 60000;

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;

    // Die übergebenen Optionsnamen über den gemeinsamen Projektor in zusätzliche
    // Erkennungsbegriffe überführen. Ohne Optionen bleibt diese Folge leer.
    private readonly FieldChoiceKeytermVocabulary _choiceKeyterms = new();

    public FormVoiceCommandFieldTranscriptCapture(
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
    }

    public async Task<FormVoiceCommandFieldTranscriptCaptureResult> CaptureAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        SpeechCaptureResult captureResult;
        try
        {
            captureResult = await CaptureFieldTranscriptAsync(turnContext, expectedInputMode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            // Bei gemeldeter fehlender Spracheingabe die erneute Ansage freigeben.
            return await HandleCaptureFailureAsync(
                    turnContext,
                    expectedInputMode,
                    FormVoiceCommandFieldErrorCodes.NoSpeechDetected,
                    autoReplayEligible: true,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Einen als vorübergehend eingestuften Aufnahmefehler als Unterbrechung
            // behandeln und dafür keine automatische Ansagewiederholung freigeben.
            return await HandleCaptureFailureAsync(
                    turnContext,
                    expectedInputMode,
                    FormVoiceCommandFieldErrorCodes.CaptureInterrupted,
                    autoReplayEligible: false,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
        {
            // Ein Ergebnis mit Fehlercode als Aufnahmeunterbrechung behandeln,
            // statt es als leeres Transkript für die Ansagewiederholung freizugeben.
            return await HandleCaptureFailureAsync(
                    turnContext,
                    expectedInputMode,
                    FormVoiceCommandFieldErrorCodes.CaptureInterrupted,
                    autoReplayEligible: false,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(captureResult.Transcript))
        {
            // Leere Transkripte für die Wiederholung freigeben, die Ergebnisarten
            // jedoch getrennt kennzeichnen: Erkennungsabschluss ohne Wörter und fehlende Eingabe.
            var issueCode = captureResult.RecognizedAsEmpty
                ? FormVoiceCommandFieldErrorCodes.RecognizedAsEmpty
                : FormVoiceCommandFieldErrorCodes.NoInput;

            return await HandleCaptureFailureAsync(
                    turnContext,
                    expectedInputMode,
                    issueCode,
                    autoReplayEligible: true,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return FormVoiceCommandFieldTranscriptCaptureResult.Captured(
            captureResult.Transcript,
            captureResult.SttConfidencePercent);
    }

    private async Task<SpeechCaptureResult> CaptureFieldTranscriptAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        CancellationToken cancellationToken)
    {
        var prompt = turnContext.FieldEntryPrompt ?? turnContext.Field.Label;
        var telemetryContext = new SpeechCaptureTelemetryContext(turnContext.Session);
        var recognitionProfile = RecognitionProfilePolicy.Resolve(expectedInputMode);
        var effectiveCaptureSettings = ResolveEffectiveCaptureSettings(turnContext);
        if (expectedInputMode == InputMode.Spelling)
        {
            var spellOutAllowed = await turnContext.PrivacyReadbackPolicy
                .IsSpellOutAllowedInConfirmationAsync(turnContext.Field.VariableId)
                .ConfigureAwait(false);
            effectiveCaptureSettings = effectiveCaptureSettings.WithInputAcceptanceFeedback(
                spellOutAllowed
                    ? SpeechInputAcceptanceFeedback.PostInterpretation
                    : SpeechInputAcceptanceFeedback.CueFree);
        }

        // Die Optionsnamen des Felds als zusätzliche Erkennungsbegriffe verwenden.
        // Welche Namen vorliegen, bestimmt die übergebene Feldprojektion.
        var choiceKeyterms = _choiceKeyterms.GetKeyterms(turnContext.Field.Options);
        return choiceKeyterms.Count == 0
            ? await _speechService.CaptureTranscriptAsync(
                prompt,
                turnContext.Context.IsoCode,
                effectiveCaptureSettings,
                onStartListening: turnContext.StateTracker.BeginListening,
                telemetryContext,
                recognitionProfile,
                keyterms: null,
                cancellationToken).ConfigureAwait(false)
            : await _speechService.CaptureTranscriptAsync(
                prompt,
                turnContext.Context.IsoCode,
                effectiveCaptureSettings,
                onStartListening: turnContext.StateTracker.BeginListening,
                telemetryContext,
                recognitionProfile,
                // Die Begriffe zusätzlich zum serverseitigen Grundvokabular anfordern.
                // Dieser Aufruf definiert kein abgeschlossenes Vokabular für den Aufnahmezug.
                new SpeechKeyterms(choiceKeyterms, KeytermComposition.WithServerBase),
                cancellationToken).ConfigureAwait(false);
    }

    private static FormVoiceCommandCaptureSettings ResolveEffectiveCaptureSettings(
        FormVoiceCommandFieldTurnContext turnContext)
    {
        if (!IsOptionListingField(turnContext.Field))
        {
            return turnContext.Context.CaptureSettings;
        }

        var timeoutMs = ResolveOptionListingInitialSilenceTimeoutMs();
        var voiceSettings = VoiceDialogSettingsRuntimeOverrides.ApplyNoInputTimeoutOverride(
            turnContext.Context.VoiceSettings,
            timeoutMs);

        return FormVoiceCommandCaptureSettings.From(voiceSettings, turnContext.Context.Contract);
    }

    private static bool IsOptionListingField(FormFieldSnapshot field)
    {
        var effectiveInteractionType = FieldInteractionSemantics.ResolveEffectiveInteractionType(
            field.CodeStyle,
            field.ValueTypeKind);

        return effectiveInteractionType is EffectiveFieldInteractionTypes.SingleSelect
            or EffectiveFieldInteractionTypes.MultiSelect;
    }

    private static int ResolveOptionListingInitialSilenceTimeoutMs()
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetOptionListingInitialSilenceTimeoutMs()
            ?? DefaultOptionListingInitialSilenceTimeoutMs;

        return Math.Clamp(
            modeled,
            MinimumOptionListingInitialSilenceTimeoutMs,
            MaximumOptionListingInitialSilenceTimeoutMs);
    }

    private async Task<FormVoiceCommandFieldTranscriptCaptureResult> HandleCaptureFailureAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        string issueCode,
        bool autoReplayEligible,
        CancellationToken cancellationToken)
    {
        // Die modellierte Zuordnung an der Ereignisgrenze bestimmt die
        // Telemetriekennung für fehlende Spracheingabe. Hier keine zusätzliche
        // Ereignismeldung erzeugen.
        var result = await _fieldErrorHandlingService.HandleCaptureFailureAsync(
                turnContext,
                expectedInputMode.ToWireValue(),
                issueCode,
                autoReplayEligible,
                cancellationToken)
            .ConfigureAwait(false);
        return FormVoiceCommandFieldTranscriptCaptureResult.Completed(result);
    }
}
