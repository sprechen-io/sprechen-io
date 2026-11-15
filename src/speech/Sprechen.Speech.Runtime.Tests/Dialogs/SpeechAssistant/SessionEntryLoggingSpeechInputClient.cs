using System.Diagnostics;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Protokolliert jeden Aufnahmeaufruf zusammen mit den Ansageaufrufen und delegiert ihn anschließend unverändert.
/// </summary>
internal sealed class SessionEntryLoggingSpeechInputClient(
    QueuedSpeechInputClient inner,
    List<(string, string, TimeSpan)> events,
    Stopwatch clock) : ISpeechInputClient, IModeAwareSpeechCaptureClient
{
    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
        => inner.RequestMicrophoneAccessAsync(cancellationToken);

    public Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken)
        => inner.EnsureCaptureSessionReadyAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => inner.StopAsync(cancellationToken);

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
    {
        Note(SpeechCaptureMode.Listening);
        return inner.RecordAsync(cancellationToken);
    }

    public Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken)
    {
        Note(captureMode);
        return inner.RecordAsync(captureMode, cancellationToken);
    }

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken)
    {
        Note(captureMode);
        return inner.RecordAsync(captureMode, initialSilenceTimeout, cancellationToken);
    }

    public Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken)
    {
        Note(captureMode);
        return inner.RecordAsync(captureMode, initialSilenceTimeout, startCursor, cancellationToken);
    }

    public SampleCursor? SnapshotLiveCaptureCursor() => inner.SnapshotLiveCaptureCursor();

    public Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
        => inner.SpeechToTextAsync(utterance, languageCode, recognitionProfile, cancellationToken, keyterms);

    private void Note(SpeechCaptureMode captureMode)
    {
        lock (events)
        {
            events.Add(("record", captureMode.ToString(), clock.Elapsed));
        }
    }
}
