namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bindet eine lokale Sitzungsnutzung an ihre Generation und Abschlussaufgaben,
/// damit mehrfaches Freigeben oder ein alter Nutzer die aktuelle Nutzung nicht verändert.
/// </summary>
internal sealed class SpeechCaptureLocalSessionLease : IAsyncDisposable
{
    private readonly SpeechCaptureLocalSessionLifecycle _owner;
    private readonly long _generation;
    private int _disposed;

    public SpeechCaptureLocalSessionLease(
        SpeechCaptureLocalSessionLifecycle owner,
        SpeechCaptureSessionHandle session,
        long generation)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        MimeType = session.MimeType;
        SessionCompletion = session.Completion ?? throw new ArgumentNullException(nameof(session));
        RecordingCompletion = session.RecordingCompletion ?? throw new ArgumentNullException(nameof(session));
        _generation = generation;
    }

    public SpeechAudioMimeType MimeType { get; }

    internal Task SessionCompletion { get; }

    internal Task RecordingCompletion { get; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _owner.ReleaseAsync(_generation).ConfigureAwait(false);
    }
}
