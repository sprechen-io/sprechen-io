namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Audioerfassung und Transkription für die Aktivierungserkennung.
/// Die Erkennung anhand von Text benötigt eine fortlaufende Segmentquelle.
/// Kurze Aufnahmen beim Sitzungseinstieg gehören nicht zu diesem Bündel.
/// </summary>
public sealed class WakeRuntimeCaptureDependencies
{
    public WakeRuntimeCaptureDependencies(
        SpeechRecognitionWorker speechRecognitionWorker,
        IContinuousSegmentStreamSource segmentStreamSource)
    {
        SpeechRecognitionWorker = speechRecognitionWorker ?? throw new ArgumentNullException(nameof(speechRecognitionWorker));
        SegmentStreamSource = segmentStreamSource ?? throw new ArgumentNullException(nameof(segmentStreamSource));
    }

    public SpeechRecognitionWorker SpeechRecognitionWorker { get; }

    public IContinuousSegmentStreamSource SegmentStreamSource { get; }
}
