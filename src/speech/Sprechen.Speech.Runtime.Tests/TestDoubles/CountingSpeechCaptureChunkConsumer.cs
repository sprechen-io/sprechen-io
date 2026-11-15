namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Zählt übergebene Chunks, ohne sie zu speichern oder neue Tasks anzulegen.
/// So bleibt der Beitrag des Consumers zur Allokationsmessung klein.
/// </summary>
internal sealed class CountingSpeechCaptureChunkConsumer : ISpeechCaptureChunkConsumer
{
    public int ChunkCount { get; private set; }

    public Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        ChunkCount++;
        return Task.CompletedTask;
    }

    public void FailActiveCapture(Exception ex)
    {
    }

    public void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
    }
}
