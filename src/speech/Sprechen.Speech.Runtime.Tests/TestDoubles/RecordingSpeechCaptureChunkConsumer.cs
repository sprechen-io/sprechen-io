namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingSpeechCaptureChunkConsumer : ISpeechCaptureChunkConsumer
{
    private readonly List<byte[]> _chunks = [];
    private readonly Action<RecordingSpeechCaptureChunkConsumer>? _onChunk;

    public RecordingSpeechCaptureChunkConsumer(Action<RecordingSpeechCaptureChunkConsumer>? onChunk = null)
    {
        _onChunk = onChunk;
    }

    public IReadOnlyList<byte[]> Chunks => _chunks;

    public int FailureCount { get; private set; }

    public int CancellationCount { get; private set; }

    public Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        _chunks.Add(chunk);
        _onChunk?.Invoke(this);
        return Task.CompletedTask;
    }

    public void FailActiveCapture(Exception ex) => FailureCount++;

    public void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken) => CancellationCount++;
}
