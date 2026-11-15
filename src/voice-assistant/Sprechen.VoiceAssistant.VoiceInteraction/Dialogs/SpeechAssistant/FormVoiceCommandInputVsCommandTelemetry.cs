namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet und protokolliert die Auflösungsdaten für Eingabe-oder-Befehl-Signale, damit angenommene Feldinhalte ihren Interpretationskontext mitführen können.
/// </summary>
internal sealed class FormVoiceCommandInputVsCommandTelemetry
{
    private const string AmbiguityTypeInputVsCommand = "input_vs_command";

    private readonly IEvaluationCommandTelemetry _evaluationTelemetry;
    private readonly InputVsCommandResolutionStrategyMapper _resolutionStrategyMapper;

    public FormVoiceCommandInputVsCommandTelemetry(
        IEvaluationCommandTelemetry evaluationTelemetry,
        InputVsCommandResolutionStrategyMapper resolutionStrategyMapper)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _resolutionStrategyMapper = resolutionStrategyMapper ?? throw new ArgumentNullException(nameof(resolutionStrategyMapper));
    }

    public void TrackContextAccepted(
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandTranscriptInterpretation interpretation)
    {
        TrackResolved(
            session,
            field,
            interpretation,
            InputVsCommandResolutionStrategyMapper.ContextDefault,
            result: "accepted");
    }

    public AmbiguityTelemetryData CreateAcceptedData(
        FormVoiceCommandTranscriptInterpretation interpretation,
        AmbiguityResolutionResult resolution)
    {
        ArgumentNullException.ThrowIfNull(interpretation);

        if (resolution.EmitAcceptedTelemetry)
        {
            return new AmbiguityTelemetryData(
                AmbiguityType: AmbiguityTypeInputVsCommand,
                ResolutionStrategy: _resolutionStrategyMapper.Map(resolution.ResolutionStrategy),
                CommandSignalClass: interpretation.CommandSignalClass,
                Result: "accepted",
                CommandId: interpretation.ParsedCommandId);
        }

        var commandId = string.Equals(
            interpretation.CommandSignalClass,
            CommandSignalClass.NoCommand,
            StringComparison.Ordinal)
            ? null
            : (interpretation.HasParsedCommand ? interpretation.ParsedCommandId : null);

        return new AmbiguityTelemetryData(
            AmbiguityType: AmbiguityTypeInputVsCommand,
            ResolutionStrategy: InputVsCommandResolutionStrategyMapper.ContextDefault,
            CommandSignalClass: interpretation.CommandSignalClass,
            Result: "accepted",
            CommandId: commandId);
    }

    private void TrackResolved(
        EvaluationSession session,
        FormFieldSnapshot field,
        FormVoiceCommandTranscriptInterpretation interpretation,
        string resolutionStrategy,
        string result)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(interpretation);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionStrategy);
        ArgumentException.ThrowIfNullOrWhiteSpace(result);

        _evaluationTelemetry.TrackAmbiguityResolved(
            session,
            ambiguityType: AmbiguityTypeInputVsCommand,
            resolutionStrategy: resolutionStrategy,
            signalClass: interpretation.CommandSignalClass,
            result: result,
            fieldId: field.FieldId,
            commandId: interpretation.ParsedCommandId);
    }
}
