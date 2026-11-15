namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt den nächsten Interpretationszweig aus den übergebenen Befehls- und Eingabehinweisen, damit deren Vorrang an einer Stelle bestimmt wird.
/// </summary>
internal sealed class ParsedCommandBranchDecisionResolver
{
    private readonly FormVoiceCommandParsedCommandFailureDecisionResolver _failureDecisionResolver;
    private readonly FormVoiceCommandParsedCommandExecutionDecisionResolver _executionDecisionResolver;

    public ParsedCommandBranchDecisionResolver()
        : this(
            new FormVoiceCommandParsedCommandFailureDecisionResolver(),
            new FormVoiceCommandParsedCommandExecutionDecisionResolver())
    {
    }

    public ParsedCommandBranchDecisionResolver(
        FormVoiceCommandParsedCommandFailureDecisionResolver failureDecisionResolver,
        FormVoiceCommandParsedCommandExecutionDecisionResolver executionDecisionResolver)
    {
        _failureDecisionResolver = failureDecisionResolver ?? throw new ArgumentNullException(nameof(failureDecisionResolver));
        _executionDecisionResolver = executionDecisionResolver ?? throw new ArgumentNullException(nameof(executionDecisionResolver));
    }

    public ParsedCommandBranchDecision Resolve(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (_failureDecisionResolver.TryResolve(context, interpretation, out var failureDecision))
        {
            return failureDecision;
        }

        return _executionDecisionResolver.Resolve(interpretation);
    }
}
