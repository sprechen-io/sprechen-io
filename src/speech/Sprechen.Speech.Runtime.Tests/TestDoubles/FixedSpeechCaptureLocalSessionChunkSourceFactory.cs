using Sprechen.Speech.App.ClientInfrastructure.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedSpeechCaptureLocalSessionChunkSourceFactory : ISpeechCaptureLocalSessionChunkSourceFactory
{
    private readonly ISpeechAudioChunkSource _chunkSource;

    public FixedSpeechCaptureLocalSessionChunkSourceFactory(ISpeechAudioChunkSource chunkSource)
    {
        _chunkSource = chunkSource ?? throw new ArgumentNullException(nameof(chunkSource));
    }

    public int CreateCallCount { get; private set; }

    public Task<ISpeechAudioChunkSource> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateCallCount++;
        return Task.FromResult(_chunkSource);
    }
}
