using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt Ergebnisse der Aktivierungsworterkennung in Assistentenereignisse
/// mit passendem Einreihungsmodus, ohne selbst über die Erkennung zu entscheiden.
/// </summary>
internal sealed class SpeechAssistantWakeRuntimeOutcomeSink : IWakeRuntimeOutcomeSink
{
    private readonly IEventDispatcher<SpeechAssistantEvent> _dispatcher;

    public SpeechAssistantWakeRuntimeOutcomeSink(IEventDispatcher<SpeechAssistantEvent> dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public ValueTask PublishAsync(WakeRuntimeOutcome outcome, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        (SpeechAssistantEvent Event, DispatchMode DispatchMode) dispatch = outcome switch
        {
            WakeRuntimeLexicalDetectedOutcome lexical => (
                SpeechAssistantWakeWordDetectedEvent.FromLexicalEvidence(
                    lexical.Transcript,
                    lexical.CommandText,
                    lexical.CorrelationId,
                    lexical.ConfidencePercent),
                DispatchMode.Interrupt),

            WakeRuntimeAcousticDetectedOutcome acoustic => (
                SpeechAssistantWakeWordDetectedEvent.FromAcousticEvidence(
                    acoustic.Evidence,
                    acoustic.CorrelationId),
                DispatchMode.Interrupt),

            WakeRuntimeMissedOutcome missed => (
                new SpeechAssistantWakeWordMissedEvent(
                    missed.Status.ToString(),
                    missed.ConfidencePercent,
                    missed.CorrelationId),
                DispatchMode.Enqueue),

            WakeRuntimeRecognitionFailedOutcome recognitionFailed => (
                new SpeechAssistantWakeWordRecognitionFailedEvent(
                    recognitionFailed.Code,
                    recognitionFailed.Recoverable),
                DispatchMode.Interrupt),

            WakeRuntimeListenerFailedOutcome => (
                new SpeechAssistantWakeWordListenerFailedEvent(),
                DispatchMode.Interrupt),

            _ => throw new InvalidOperationException(
                $"Unsupported wake runtime outcome '{outcome.GetType().Name}'.")
        };

        return _dispatcher.DispatchAsync(dispatch.Event, dispatch.DispatchMode, cancellationToken);
    }
}
