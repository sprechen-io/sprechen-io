namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert ohne mehrdeutiges Signal keine Auflösungsentscheidung. Wenn ein
/// aktivierter Detektor dennoch ein Signal liefert, wird es zurückgewiesen,
/// da für diesen Weg kein Rückfragedialog implementiert ist.
/// </summary>
internal sealed class FormVoiceCommandAmbiguityResolver
{
    private readonly FormVoiceCommandAmbiguousSignalDetector _signalDetector;

    internal FormVoiceCommandAmbiguityResolver()
        : this(
            new FormVoiceCommandAmbiguousSignalDetector(
                new FormVoiceCommandAmbiguitySignalEligibility(),
                new FormVoiceCommandAmbiguityLabelResolver()))
    {
    }

    public FormVoiceCommandAmbiguityResolver(FormVoiceCommandAmbiguousSignalDetector signalDetector)
    {
        _signalDetector = signalDetector ?? throw new ArgumentNullException(nameof(signalDetector));
    }

    public Task<AmbiguityResolutionResult> ResolveAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandTranscriptInterpretation interpretation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (!_signalDetector.TryDetect(context, interpretation, out _))
        {
            return Task.FromResult(AmbiguityResolutionResult.None());
        }

        // Ein geliefertes Mehrdeutigkeitssignal hier ausdrücklich zurückweisen,
        // statt ohne implementierten Rückfragedialog eine Entscheidung zu erfinden.
        throw new InvalidOperationException(
            "An ambiguity signal was detected, but no clarification dialog exists for this path.");
    }
}
