using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.TestDoubles;

public sealed class RecordingSpeechService : IFormVoiceCommandSpeechService, IDeclaredAnnouncementSpeaker
{
    public List<(string Text, string IsoCode, double SpeechRate)> Calls { get; } = [];

    public Task<SpeechCaptureResult> CaptureTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<SpeechCaptureResult> CaptureTranscriptSingleAttemptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<SpeechCaptureResult> CaptureConfirmationTranscriptAsync(
        string prompt,
        string isoCode,
        FormVoiceCommandCaptureSettings captureSettings,
        Action onStartListening,
        SpeechCaptureTelemetryContext? telemetryContext,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<bool?> ConfirmYesNoAsync(
        FormVoiceCommandCaptureSettings captureSettings,
        string prompt,
        string isoCode,
        SpeechCaptureTelemetryContext? telemetryContext,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task SpeakSafeAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        AnnouncementKinds.Add(SpokenAnnouncementKind.Prompt);
        Calls.Add((text, isoCode, voiceSettings.GetSpeechRate()));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Die bei SpeakSafe und SpeakAnnouncement aufgezeichneten Ausgabekategorien.
    /// </summary>
    public List<SpokenAnnouncementKind> AnnouncementKinds { get; } = [];

    public Task SpeakAnnouncementAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        // Der Test soll die deklarierte Kategorie unabhängig vom gesprochenen Text prüfen können.
        AnnouncementKinds.Add(announcementKind);
        Calls.Add((text, isoCode, voiceSettings.GetSpeechRate()));
        return Task.CompletedTask;
    }

    public List<(string Text, string IsoCode, double SpeechRate)> ReplayCalls { get; } = [];

    public Task SpeakReplayAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        ReplayCalls.Add((text, isoCode, voiceSettings.GetSpeechRate()));
        return Task.CompletedTask;
    }
}
