using MicroSharp.Browser;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Application recording over a <see cref="BrowserCaptureLease"/>.
/// Payloads are the library session's bounded enumeration. This type has no second queue.
/// </summary>
public sealed class MicrophoneRecording : IMicrophoneRecording
{
    private readonly BrowserCaptureLease _lease;

    internal MicrophoneRecording(BrowserCaptureLease lease)
    {
        _lease = lease ?? throw new ArgumentNullException(nameof(lease));
    }

    public string MimeType => _lease.MediaType;

    public IAsyncEnumerable<byte[]> Chunks => _lease.ReadPayloadsAsync();

    public Task Completion => _lease.Completion;

    internal bool IsDisposed => _lease.HasEnded;

    /// <summary>Chunks discarded by the library capture queue.</summary>
    public long CaptureDroppedChunkCount => _lease.DroppedChunkCount;

    public ValueTask StopAsync() => StopAsync(CancellationToken.None);

    public ValueTask StopAsync(CancellationToken cancellationToken) => _lease.StopAsync(cancellationToken);

    public ValueTask DisposeAsync() => _lease.DisposeAsync();
}
