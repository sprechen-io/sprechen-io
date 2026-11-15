using Sprechen.Speech.App.Contracts.Services;
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erfasst einen buchstabierten Wert in mehreren Aufnahmen. Pro Aufruf
/// entsteht eine neue <see cref="SpellingAccumulationStrategy"/> mit eigenem
/// Puffer. <see cref="SpellingBlockReadback"/> berücksichtigt die Vorleseregeln
/// des Felds. <see cref="CharacterAccumulationWorkflowDriver"/> steuert
/// Aufnahme, Fortsetzung, Fehlerbehandlung und Abschluss.
/// </summary>
internal sealed class FormVoiceCommandSpellingWorkflowService
{
    private readonly CharacterAccumulationWorkflowDriver _driver;
    private readonly CharacterAccumulationAnchoredCommandFlow _anchoredCommandFlow;
    private readonly SpellingWorkflowCommandResolver _commandResolver;
    private readonly IDialogDecisionSink _decisionSink;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly SpellingWorkflowCommandActionFlow _commandActionFlow;
    private readonly SpellingWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly IPhoneticSpellOutRenderer _phoneticSpellOutRenderer;

    public FormVoiceCommandSpellingWorkflowService(
        CharacterAccumulationWorkflowDriver driver,
        CharacterAccumulationAnchoredCommandFlow anchoredCommandFlow,
        SpellingWorkflowCommandResolver commandResolver,
        IFormTranscriptParser transcriptParser,
        IFormVoiceCommandSpeechService speechService,
        SpellingWorkflowCommandActionFlow commandActionFlow,
        SpellingWorkflowInputRejectionFlow inputRejectionFlow,
        IPhoneticSpellOutRenderer phoneticSpellOutRenderer,
        IDialogDecisionSink? decisionSink = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _anchoredCommandFlow = anchoredCommandFlow ?? throw new ArgumentNullException(nameof(anchoredCommandFlow));
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _decisionSink = decisionSink ?? NullDialogDecisionSink.Instance;
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _phoneticSpellOutRenderer = phoneticSpellOutRenderer ?? throw new ArgumentNullException(nameof(phoneticSpellOutRenderer));
    }

    public async Task<CharacterAccumulationResult> CaptureSpellingTranscriptAsync(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy,
        string initialTranscript,
        int? initialConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(escalation);
        ArgumentNullException.ThrowIfNull(privacyReadbackPolicy);

        var blockReadback = new SpellingBlockReadback(
            _speechService,
            privacyReadbackPolicy,
            _phoneticSpellOutRenderer);

        var spellOutAllowed = await privacyReadbackPolicy
            .IsSpellOutAllowedInConfirmationAsync(field.VariableId)
            .ConfigureAwait(false);
        var inputAcceptanceFeedback = spellOutAllowed
            ? SpeechInputAcceptanceFeedback.PostInterpretation
            : SpeechInputAcceptanceFeedback.CueFree;

        var strategy = new SpellingAccumulationStrategy(
            _commandResolver,
            _transcriptParser,
            _speechService,
            _commandActionFlow,
            _inputRejectionFlow,
            blockReadback,
            inputAcceptanceFeedback,
            _decisionSink,
            initialConfidencePercent);

        return await _driver.RunAsync(
            strategy,
            new CharacterAccumulationContext(context, interpreter, field, session, stateTracker, autoHelp, escalation, _anchoredCommandFlow),
            initialTranscript,
            cancellationToken).ConfigureAwait(false);
    }
}
