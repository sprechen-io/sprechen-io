using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Zählt empfangene Gesprächsereignisse und nichtleere Transkriptaktualisierungen
/// sowie Abschlusstranskripte und merkt sich einen gemeldeten Gesprächsfehler.
/// </summary>
internal sealed class SpeechToTextStreamingReceiveState
{
    public int EventCount { get; private set; }

    public int TranscriptCount { get; private set; }

    public Exception? TerminalFailure { get; private set; }

    public void Apply(SpeechConversationEvent conversationEvent)
    {
        ArgumentNullException.ThrowIfNull(conversationEvent);

        EventCount++;
        if (HasTranscript(conversationEvent))
        {
            TranscriptCount++;
        }

        if (conversationEvent is SpeechConversationFailedEvent failed)
        {
            TerminalFailure = CreateTerminalFailure(failed);
        }
    }

    private static bool HasTranscript(SpeechConversationEvent conversationEvent)
        => conversationEvent switch
        {
            SpeechConversationTranscriptUpdateEvent update => !string.IsNullOrWhiteSpace(update.Transcript),
            SpeechConversationTranscriptFinalizedEvent finalized => !string.IsNullOrWhiteSpace(finalized.Transcript),
            _ => false
        };

    private static Exception CreateTerminalFailure(SpeechConversationFailedEvent failed)
        => new InvalidOperationException(
            string.IsNullOrWhiteSpace(failed.Detail)
                ? "Speech conversation failed."
                : failed.Detail);
}
