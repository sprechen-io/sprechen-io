using Sprechen.Mda.Contracts.V1.Telemetry;
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beantwortet ein alleinstehendes Aktivierungswort mit einem Hinweis zur
/// Befehlsform. Andere nicht ausgeführte Transkripte werden zur Feldverarbeitung
/// weitergegeben.
/// </summary>
internal sealed class FormVoiceCommandAmbiguityTranscriptBranch
{
    private readonly FormVoiceCommandAmbiguityResolver _ambiguityResolver;
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;
    private readonly FormVoiceCommandInputVsCommandTelemetry _ambiguityTelemetry;

    public FormVoiceCommandAmbiguityTranscriptBranch(
        FormVoiceCommandAmbiguityResolver ambiguityResolver,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService,
        FormVoiceCommandInputVsCommandTelemetry ambiguityTelemetry)
    {
        _ambiguityResolver = ambiguityResolver ?? throw new ArgumentNullException(nameof(ambiguityResolver));
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
        _ambiguityTelemetry = ambiguityTelemetry ?? throw new ArgumentNullException(nameof(ambiguityTelemetry));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult> ResolveAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        // Ein alleinstehendes akzeptiertes Aktivierungswort als unvollständigen
        // Befehl behandeln und einen Formulierungshinweis anfordern, statt es
        // als Feldwert zu übernehmen.
        if (FormGlobalCommandParser.IsBareCommandAnchor(
                interpretation.TrimmedTranscript,
                turnContext.Context.AcceptedCommandAnchorForms))
        {
            var anchorHintPrompt = FormCommandAnchorHintPromptBuilder.BuildPrompt(
                turnContext.Context.Contract,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings.GetPolitenessLevel());
            var bareAnchorResult = await _fieldErrorHandlingService.HandlePromptedFailureAsync(
                    turnContext,
                    FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                    anchorHintPrompt,
                    TelemetryErrorTriggers.SystemRecognition,
                    cancellationToken)
                .ConfigureAwait(false);
            return FormVoiceCommandFieldOrchestrationResult.Completed(bareAnchorResult);
        }

        var ambiguityResolution = await _ambiguityResolver.ResolveAsync(
                turnContext.Context,
                turnContext.Session,
                turnContext.StateTracker,
                interpretation,
                cancellationToken)
            .ConfigureAwait(false);

        // Ohne Mehrdeutigkeitsentscheidung das Transkript mit Erkennungssicherheit
        // und Telemetriedaten an die weitere Feldverarbeitung geben.
        return FormVoiceCommandFieldOrchestrationResult.ContinueWithTranscript(
            interpretation.InputTranscript,
            _ambiguityTelemetry.CreateAcceptedData(interpretation, ambiguityResolution),
            sttConfidencePercent);
    }
}
