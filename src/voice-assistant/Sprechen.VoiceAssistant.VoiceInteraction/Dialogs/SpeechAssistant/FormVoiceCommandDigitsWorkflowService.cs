namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erfasst einen Ziffernwert in mehreren Aufnahmen. Pro Aufruf entsteht eine
/// neue <see cref="DigitsAccumulationStrategy"/> mit eigenem Puffer.
/// <see cref="CharacterAccumulationWorkflowDriver"/> steuert die gemeinsame
/// Schleife für Aufnahme, Fortsetzung und Abschluss.
/// </summary>
internal sealed class FormVoiceCommandDigitsWorkflowService
{
    private readonly CharacterAccumulationWorkflowDriver _driver;
    private readonly CharacterAccumulationAnchoredCommandFlow _anchoredCommandFlow;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly DigitsWorkflowCommandResolver _commandResolver;
    private readonly DigitsWorkflowCommandActionFlow _commandActionFlow;
    private readonly DigitsWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly DigitsWorkflowTranscriptCapture _transcriptCapture;

    public FormVoiceCommandDigitsWorkflowService(
        CharacterAccumulationWorkflowDriver driver,
        CharacterAccumulationAnchoredCommandFlow anchoredCommandFlow,
        IFormTranscriptParser transcriptParser,
        DigitsWorkflowCommandResolver commandResolver,
        DigitsWorkflowCommandActionFlow commandActionFlow,
        DigitsWorkflowInputRejectionFlow inputRejectionFlow,
        DigitsWorkflowTranscriptCapture transcriptCapture)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _anchoredCommandFlow = anchoredCommandFlow ?? throw new ArgumentNullException(nameof(anchoredCommandFlow));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _transcriptCapture = transcriptCapture ?? throw new ArgumentNullException(nameof(transcriptCapture));
    }

    public Task<CharacterAccumulationResult> CaptureDigitsTranscriptAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        string initialTranscript,
        int? initialConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escalation);

        var strategy = new DigitsAccumulationStrategy(
            _transcriptParser,
            _commandResolver,
            _commandActionFlow,
            _inputRejectionFlow,
            _transcriptCapture,
            initialConfidencePercent);

        return _driver.RunAsync(
            strategy,
            new CharacterAccumulationContext(context, interpreter, field, session, stateTracker, autoHelp, escalation, _anchoredCommandFlow),
            initialTranscript,
            cancellationToken);
    }
}
