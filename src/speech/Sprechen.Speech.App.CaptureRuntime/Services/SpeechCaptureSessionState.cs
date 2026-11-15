namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Hält Mikrofonzugriff, Aufnahme und Abschlussaufgaben einer lokalen Sitzung.
/// Nach Freigabe wird der Zugriff erneut angefordert. Laufende Sitzungen können wiederverwendet werden.
/// </summary>
internal sealed class SpeechCaptureSessionState
{
    private bool _microphoneAccessRequested;
    private CancellationTokenSource? _sessionCts;
    private Task? _sessionTask;
    private Task? _sessionCompletion;
    private Task? _recordingCompletion;
    private IMicrophoneRecording? _recording;
    private SpeechAudioMimeType _microphoneMimeType;

    internal bool MicrophoneAccessRequested => Volatile.Read(ref _microphoneAccessRequested);

    internal void MarkMicrophoneAccessRequested()
    {
        Volatile.Write(ref _microphoneAccessRequested, true);
    }

    internal void ResetMicrophoneAccess()
    {
        // Nach der Gerätefreigabe muss der nächste Zugriff erneut angefordert werden.
        Volatile.Write(ref _microphoneAccessRequested, false);
    }

    internal bool TryGetWarmSession(out SpeechCaptureSessionHandle session)
    {
        var sessionCompletion = Volatile.Read(ref _sessionCompletion);
        var recordingCompletion = Volatile.Read(ref _recordingCompletion);
        if (sessionCompletion is not null
            && recordingCompletion is not null
            && !sessionCompletion.IsCompleted
            && !recordingCompletion.IsCompleted)
        {
            session = new SpeechCaptureSessionHandle(
                _microphoneMimeType,
                sessionCompletion,
                recordingCompletion);
            return true;
        }

        session = default;
        return false;
    }

    internal void Start(
        CancellationTokenSource sessionCts,
        IMicrophoneRecording recording,
        SpeechAudioMimeType mimeType,
        Task sessionTask,
        Task sessionCompletion,
        Task recordingCompletion)
    {
        _sessionCts = sessionCts ?? throw new ArgumentNullException(nameof(sessionCts));
        _recording = recording ?? throw new ArgumentNullException(nameof(recording));
        _microphoneMimeType = mimeType;
        _sessionTask = sessionTask ?? throw new ArgumentNullException(nameof(sessionTask));
        _sessionCompletion = sessionCompletion ?? throw new ArgumentNullException(nameof(sessionCompletion));
        _recordingCompletion = recordingCompletion ?? throw new ArgumentNullException(nameof(recordingCompletion));
    }

    internal SpeechCaptureSessionSnapshot Clear()
    {
        var snapshot = new SpeechCaptureSessionSnapshot(
            _sessionTask,
            _sessionCts,
            _recording,
            _microphoneMimeType);

        _sessionTask = null;
        _sessionCompletion = null;
        _recordingCompletion = null;
        _sessionCts = null;
        _recording = null;
        _microphoneMimeType = default;
        return snapshot;
    }
}
