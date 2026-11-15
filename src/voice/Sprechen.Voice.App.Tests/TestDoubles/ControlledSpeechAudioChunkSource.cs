using System.Threading.Channels;
using Sprechen.Speech.App.ClientInfrastructure.Services;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class ControlledSpeechAudioChunkSource : ISpeechAudioChunkSource
{
    private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly TaskCompletionSource _stopCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _gate = new();

    private Task? _stopTask;

    public ControlledSpeechAudioChunkSource(SpeechAudioMimeType mimeType)
    {
        MimeType = mimeType;
    }

    public bool AutoCompleteOnStop { get; set; } = true;

    public int StopCallCount { get; private set; }

    public int DisposeCallCount { get; private set; }

    public SpeechAudioMimeType MimeType { get; }

    public IAsyncEnumerable<byte[]> Chunks => _chunks.Reader.ReadAllAsync();

    public void PushChunk(byte[] chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        _chunks.Writer.TryWrite(chunk);
    }

    public void Complete()
    {
        _chunks.Writer.TryComplete();
        _stopCompletion.TrySetResult();
    }

    public void Fail(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        _chunks.Writer.TryComplete(ex);
        _stopCompletion.TrySetException(ex);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Task stopTask;
        lock (_gate)
        {
            stopTask = _stopTask ??= StopCoreAsync();
        }

        return stopTask.WaitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        DisposeCallCount++;
        Complete();
        return ValueTask.CompletedTask;
    }

    private async Task StopCoreAsync()
    {
        StopCallCount++;

        if (AutoCompleteOnStop)
        {
            Complete();
        }

        await _stopCompletion.Task.ConfigureAwait(false);
    }
}
