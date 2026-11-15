using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Erfassungsfehler von regulärem Ablauf ohne Sprache,
/// damit Wiederholungen bei Fehlern gezielt verzögert werden können.
/// Ein angenommener Zuhörstart enthält ebenfalls noch kein Audio.
/// </summary>
/// <param name="Audio">Erfasstes Sprachsegment oder <c>null</c> bei Ablauf ohne
/// Sprache, angenommenem Zuhörstart oder Erfassungsfehler.</param>
/// <param name="Failed">Kennzeichnet einen fehlgeschlagenen Erfassungsversuch.
/// Bei Segment, regulärem Ablauf ohne Sprache oder angenommenem Start <c>false</c>.</param>
/// <param name="UnavailableReason">Klassifizierte Mikrofonursache für einen
/// fehlgeschlagenen Erfassungsversuch.</param>
internal readonly record struct WakeCaptureCycleOutcome(
    CapturedSpeechAudio? Audio,
    bool Failed,
    WakeCaptureUnavailableReason UnavailableReason = WakeCaptureUnavailableReason.None)
{
    public static WakeCaptureCycleOutcome Captured(CapturedSpeechAudio audio) => new(audio, Failed: false);

    public static WakeCaptureCycleOutcome NoSpeech { get; } = new(Audio: null, Failed: false);

    /// <summary>
    /// Die fortlaufende Segmentquelle hat den Zuhörstart angenommen. Audio liegt noch nicht vor.
    /// </summary>
    public static WakeCaptureCycleOutcome ListeningStarted { get; } = new(Audio: null, Failed: false);

    public static WakeCaptureCycleOutcome CaptureFailedWith(WakeCaptureUnavailableReason reason)
        => new(Audio: null, Failed: true, reason);
}
