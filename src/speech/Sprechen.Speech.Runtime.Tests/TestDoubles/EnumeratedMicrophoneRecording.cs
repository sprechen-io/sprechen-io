using System.Runtime.CompilerServices;
using Sprechen.Speech.App.CaptureRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Liefert vorbereitete Chunks und beendet danach den Stream, um ein Aufnahmeende
/// nachzubilden. Vor jedem Chunk prüft der Iterator den Abbruchtoken.
/// </summary>
internal sealed class EnumeratedMicrophoneRecording : IMicrophoneRecording
{
    private readonly IReadOnlyList<byte[]> _chunks;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public EnumeratedMicrophoneRecording(string mimeType, IReadOnlyList<byte[]> chunks)
    {
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
    }

    public string MimeType { get; }

    public IAsyncEnumerable<byte[]> Chunks => EnumerateAsync();

    public Task Completion => _completion.Task;

    public ValueTask DisposeAsync()
    {
        _completion.TrySetResult();
        return ValueTask.CompletedTask;
    }

    private async IAsyncEnumerable<byte[]> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return chunk;
                await Task.Yield();
            }
        }
        finally
        {
            _completion.TrySetResult();
        }
    }
}
