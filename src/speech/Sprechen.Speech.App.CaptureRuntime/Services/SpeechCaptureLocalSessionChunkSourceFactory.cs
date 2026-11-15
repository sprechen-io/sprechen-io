namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet eine lokale Sitzungsnutzung mit dem Blockverteiler,
/// damit die erzeugte Audioquelle ihre Nutzung bei Beendigung wieder freigeben kann.
/// </summary>
internal sealed class SpeechCaptureLocalSessionChunkSourceFactory(
    SpeechCaptureLocalSessionLifecycle localSessionLifecycle,
    SpeechCaptureChunkDispatcher chunkDispatcher) : ISpeechCaptureLocalSessionChunkSourceFactory
{
    private readonly SpeechCaptureLocalSessionLifecycle _localSessionLifecycle =
        localSessionLifecycle ?? throw new ArgumentNullException(nameof(localSessionLifecycle));
    private readonly SpeechCaptureChunkDispatcher _chunkDispatcher =
        chunkDispatcher ?? throw new ArgumentNullException(nameof(chunkDispatcher));

    public async Task<ISpeechAudioChunkSource> CreateAsync(CancellationToken cancellationToken)
    {
        var lease = await _localSessionLifecycle.AcquireAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return new SpeechCaptureLocalSessionChunkSource(_chunkDispatcher, lease);
        }
        catch
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
