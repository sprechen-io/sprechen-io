namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erfasst eine Mehrfachauswahl in mehreren Aufnahmen. Pro Aufruf entsteht
/// eine neue <see cref="SetAccumulationStrategy"/> mit eigenem Auswahlpuffer.
/// <see cref="CharacterAccumulationWorkflowDriver"/> steuert die gemeinsame
/// Schleife für Aufnahme, Fortsetzung und Abschluss. Weitere Optionen
/// werden der bisherigen Auswahl hinzugefügt.
/// </summary>
internal sealed class FormVoiceCommandSetWorkflowService
{
    private readonly CharacterAccumulationWorkflowDriver _driver;
    private readonly CharacterAccumulationAnchoredCommandFlow _anchoredCommandFlow;
    private readonly SetWorkflowCommandResolver _commandResolver;
    private readonly SetWorkflowCommandActionFlow _commandActionFlow;
    private readonly SetWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandSetWorkflowService(
        CharacterAccumulationWorkflowDriver driver,
        CharacterAccumulationAnchoredCommandFlow anchoredCommandFlow,
        SetWorkflowCommandResolver commandResolver,
        SetWorkflowCommandActionFlow commandActionFlow,
        SetWorkflowInputRejectionFlow inputRejectionFlow,
        IFormTranscriptParser transcriptParser,
        IFormVoiceCommandSpeechService speechService)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _anchoredCommandFlow = anchoredCommandFlow ?? throw new ArgumentNullException(nameof(anchoredCommandFlow));
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public Task<CharacterAccumulationResult> CaptureSetTranscriptAsync(
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(escalation);

        var strategy = new SetAccumulationStrategy(
            _commandResolver,
            _commandActionFlow,
            _inputRejectionFlow,
            _transcriptParser,
            _speechService,
            field.Options,
            initialConfidencePercent);

        return _driver.RunAsync(
            strategy,
            new CharacterAccumulationContext(context, interpreter, field, session, stateTracker, autoHelp, escalation, _anchoredCommandFlow),
            initialTranscript,
            cancellationToken);
    }
}
