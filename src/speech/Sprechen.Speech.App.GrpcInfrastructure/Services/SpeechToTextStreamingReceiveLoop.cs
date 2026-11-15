using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.App.Contracts.Services;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überträgt empfangene gRPC-Gesprächsereignisse in den Ereigniskanal und
/// schließt ihn je nach Streamausgang regulär oder mit dem festgestellten Fehler.
/// </summary>
internal sealed class SpeechToTextStreamingReceiveLoop
{
    private readonly SpeechToTextStreamingEventChannel _events;
    private readonly SpeechToTextStreamingSessionTrace _trace;
    private readonly CancellationToken _sessionCancellationToken;

    internal SpeechToTextStreamingReceiveLoop(
        SpeechToTextStreamingEventChannel events,
        SpeechToTextStreamingSessionTrace trace,
        CancellationToken sessionCancellationToken)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _sessionCancellationToken = sessionCancellationToken;
    }

    internal async Task RunAsync(
        IAsyncStreamReader<GrpcSpeechConversationEvent> responseStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responseStream);

        var state = new SpeechToTextStreamingReceiveState();
        try
        {
            while (await responseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var conversationEvent = SpeechConversationEventGrpcMapper.FromProto(responseStream.Current);
                state.Apply(conversationEvent);
                await _events.WriteAsync(conversationEvent, cancellationToken).ConfigureAwait(false);
            }

            if (state.TerminalFailure is not null)
            {
                await _trace.ReceiveFailedEventAsync(state.EventCount, state.TranscriptCount).ConfigureAwait(false);
                _events.Complete(state.TerminalFailure);
                throw state.TerminalFailure;
            }

            await _trace.ReceiveCompletedAsync(state.EventCount, state.TranscriptCount).ConfigureAwait(false);
            _events.Complete();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            await _trace.ReceiveCanceledAsync(ex.StatusCode, state.EventCount, state.TranscriptCount).ConfigureAwait(false);
            _events.Complete();
        }
        catch (RpcException ex)
        {
            var mappedException = SpeechErrorExceptionFactory.Create(ex, _sessionCancellationToken);
            await _trace.ReceiveFailedRpcAsync(ex.StatusCode, state.EventCount, state.TranscriptCount).ConfigureAwait(false);
            _events.Complete(mappedException);
            throw mappedException;
        }
        catch (OperationCanceledException)
        {
            await _trace.ReceiveCanceledAsync(state.EventCount, state.TranscriptCount).ConfigureAwait(false);
            _events.Complete();
        }
        catch (Exception ex)
        {
            await _trace.ReceiveFailedUnexpectedAsync(state.EventCount, state.TranscriptCount, ex.GetType().Name).ConfigureAwait(false);
            _events.Complete(ex);
            throw;
        }
    }
}
