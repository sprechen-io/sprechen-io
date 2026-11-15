namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erfasst Audio in einem gewählten Modus. Ein Startcursor ermöglicht,
/// Audio aus der Wartezeit nach einer Ansage in die Erfassung einzubeziehen.
/// </summary>
public interface IModeAwareSpeechCaptureClient
{
    Task<CapturedSpeechAudio> RecordAsync(SpeechCaptureMode captureMode, CancellationToken cancellationToken);

    Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        CancellationToken cancellationToken);

    /// <summary>
    /// Merkt die aktuelle Schreibposition des Sample-Rings, damit später auch
    /// die während einer Wartezeit eingetroffenen Audiodaten verarbeitet werden können.
    /// Implementierungen ohne Ring, etwa Testdoubles, können null zurückgeben.
    /// </summary>
    SampleCursor? SnapshotLiveCaptureCursor();

    /// <summary>
    /// Beginnt die Erfassung am übergebenen <see cref="SampleCursor"/> und berücksichtigt
    /// damit auch Audio, das zwischen Cursoraufnahme und Erfassungsbeginn eingetroffen ist.
    /// </summary>
    /// <remarks>
    /// Die Produktionsimplementierung muss den Cursor verwenden. Testdoubles ohne Ring
    /// dokumentieren ausdrücklich, wenn sie ihn nicht nachbilden.
    /// </remarks>
    Task<CapturedSpeechAudio> RecordAsync(
        SpeechCaptureMode captureMode,
        TimeSpan initialSilenceTimeout,
        SampleCursor startCursor,
        CancellationToken cancellationToken);
}
