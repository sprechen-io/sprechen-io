namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecorderBackedCaptureClient(IMicrophoneRecorder recorder)
    : IModeAwareSpeechCaptureClient
{
    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        CancellationToken cancellationToken) => RecordAsync(
            captureMode,
            TimeSpan.FromSeconds(5),
            cancellationToken);

    public async Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
    {
        await using var recording = await recorder.StartAsync(cancellationToken);
        return new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, []);
    }

    public SampleCursor? SnapshotLiveCaptureCursor() => null;

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken) => RecordAsync(
            captureMode,
            initialSilenceTimeout,
            cancellationToken);
}
