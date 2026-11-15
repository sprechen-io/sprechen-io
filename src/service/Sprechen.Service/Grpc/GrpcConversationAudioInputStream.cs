using Grpc.Core;
using System.Runtime.CompilerServices;
using System.Threading;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Liest gepuffertes und nachfolgendes Gesprächsaudio.
/// Bleiben nicht leere Segmente aus, wird der wartende Leseaufruf nach dem Zeitlimit abgebrochen.
/// </summary>
internal sealed class GrpcConversationAudioInputStream
{
    private readonly TimeSpan _clientInactivityTimeout;
    private int _enumerationStarted;
    private int _completionReason;

    public GrpcConversationAudioInputStream(TimeSpan clientInactivityTimeout)
    {
        if (clientInactivityTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(clientInactivityTimeout),
                clientInactivityTimeout,
                "Client inactivity timeout must be positive.");
        }

        _clientInactivityTimeout = clientInactivityTimeout;
    }

    public bool CompletedByInactivityTimeout => CompletionReason == GrpcConversationAudioInputCompletionReason.InactivityTimeout;

    public GrpcConversationAudioInputCompletionReason CompletionReason
        => (GrpcConversationAudioInputCompletionReason)Volatile.Read(ref _completionReason);

    public async IAsyncEnumerable<byte[]> ReadAllChunksAsync(
        IReadOnlyList<byte[]> bufferedChunks,
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(bufferedChunks);
        ArgumentNullException.ThrowIfNull(requestStream);

        if (Interlocked.Exchange(ref _enumerationStarted, 1) != 0)
        {
            throw new InvalidOperationException("Conversation audio input streams can only be enumerated once.");
        }

        foreach (var chunk in bufferedChunks)
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            yield return chunk;
        }

        var inactivityDeadlineUtc = DateTime.UtcNow + _clientInactivityTimeout;
        while (await MoveNextBeforeInactivityTimeoutAsync(requestStream, inactivityDeadlineUtc, ct).ConfigureAwait(false))
        {
            var chunk = requestStream.Current;
            if (chunk.Data is null || chunk.Data.IsEmpty)
            {
                continue;
            }

            inactivityDeadlineUtc = DateTime.UtcNow + _clientInactivityTimeout;
            yield return chunk.Data.ToByteArray();
        }

        MarkNaturalCompletion();
    }

    private async Task<bool> MoveNextBeforeInactivityTimeoutAsync(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        DateTime inactivityDeadlineUtc,
        CancellationToken ct)
    {
        var remaining = inactivityDeadlineUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            MarkInactivityTimeoutCompletion();
            return false;
        }

        using var moveNextCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var timeoutCts = new CancellationTokenSource();

        var moveNextTask = requestStream.MoveNext(moveNextCts.Token);
        var timeoutTask = Task.Delay(remaining, timeoutCts.Token);
        var completedTask = await Task.WhenAny(moveNextTask, timeoutTask).ConfigureAwait(false);

        if (completedTask == moveNextTask)
        {
            timeoutCts.Cancel();
            return await moveNextTask.ConfigureAwait(false);
        }

        MarkInactivityTimeoutCompletion();
        moveNextCts.Cancel();

        try
        {
            await moveNextTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (moveNextCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // Das Inaktivitätszeitlimit hat den wartenden MoveNext-Aufruf abgebrochen.
        }

        return false;
    }

    private void MarkNaturalCompletion()
    {
        _ = Interlocked.CompareExchange(
            ref _completionReason,
            (int)GrpcConversationAudioInputCompletionReason.Natural,
            (int)GrpcConversationAudioInputCompletionReason.None);
    }

    private void MarkInactivityTimeoutCompletion()
    {
        _ = Interlocked.CompareExchange(
            ref _completionReason,
            (int)GrpcConversationAudioInputCompletionReason.InactivityTimeout,
            (int)GrpcConversationAudioInputCompletionReason.None);
    }
}
