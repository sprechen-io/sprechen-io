using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.App.Contracts.Services;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Verbindet lokale Audioaufnahme, gRPC-Schleifen und Ereigniskanal und teilt laufende Abschlussaufgaben,
/// damit parallele Aufrufer denselben Stopp oder dieselbe Freigabe abwarten.
/// </summary>
internal sealed class SpeechToTextStreamingSession : ISpeechToTextStreamingSession
{
    private static readonly TimeSpan DefaultStopGrace = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private readonly CancellationTokenSource _sessionCts;
    private readonly SpeechToTextStreamingEventChannel _events;
    private readonly SpeechToTextStreamingSessionTrace _trace;
    private readonly GrpcDuplexStreamTeardown _teardown;

    private ISpeechAudioChunkSource? _audioSource;
    private AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>? _call;
    private Task? _sendTask;
    private Task? _receiveTask;
    private Task? _stopTask;
    private Task? _disposeTask;

    public SpeechToTextStreamingSession(
        ISpeechAudioChunkSource audioSource,
        AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent> call,
        string? languageCode,
        string correlationId,
        ISpeechClientTrace trace,
        CancellationToken cancellationToken)
        : this(audioSource, call, languageCode, correlationId, trace, DefaultStopGrace, cancellationToken)
    {
    }

    public SpeechToTextStreamingSession(
        ISpeechAudioChunkSource audioSource,
        AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent> call,
        string? languageCode,
        string correlationId,
        ISpeechClientTrace trace,
        TimeSpan stopGrace,
        CancellationToken cancellationToken)
    {
        _teardown = new GrpcDuplexStreamTeardown(stopGrace);

        var normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim();

        _audioSource = audioSource ?? throw new ArgumentNullException(nameof(audioSource));
        _call = call ?? throw new ArgumentNullException(nameof(call));
        _trace = new SpeechToTextStreamingSessionTrace(trace, correlationId, normalizedLanguageCode);

        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _events = new SpeechToTextStreamingEventChannel();

        _sendTask = new GrpcAudioChunkSendLoop().WriteAsync(
            _audioSource.Chunks,
            _call.RequestStream,
            _audioSource.MimeType,
            _sessionCts.Token,
            languageCode: normalizedLanguageCode);

        var receiveLoop = new SpeechToTextStreamingReceiveLoop(_events, _trace, _sessionCts.Token);
        _receiveTask = receiveLoop.RunAsync(_call.ResponseStream, _sessionCts.Token);
        _ = _trace.StartedAsync();
    }

    public IAsyncEnumerable<SpeechConversationEvent> Events => _events.Events;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task stopTask;
        lock (_gate)
        {
            stopTask = _stopTask ??= StopCoreAsync();
        }

        await stopTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        Task? stopTask;
        lock (_gate)
        {
            stopTask = _stopTask;
        }

        if (stopTask is not null)
        {
            await _teardown.AwaitInFlightStopWithGraceAsync(stopTask, _sessionCts).ConfigureAwait(false);
            return;
        }

        try
        {
            _sessionCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        await DisposeOnceAsync().ConfigureAwait(false);
    }

    private async Task StopCoreAsync()
    {
        try
        {
            await _trace.StoppingAsync().ConfigureAwait(false);

            if (_audioSource is not null)
            {
                await _audioSource.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }

            await _teardown.AwaitSendReceiveWithGraceAsync(_sendTask, _receiveTask, _sessionCts).ConfigureAwait(false);

            await _trace.StopCompletedAsync().ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            await _trace.StopFailedAsync(ex.StatusCode).ConfigureAwait(false);
            throw SpeechErrorExceptionFactory.Create(ex, _sessionCts.Token);
        }
        finally
        {
            await DisposeOnceAsync().ConfigureAwait(false);
        }
    }

    private Task DisposeOnceAsync()
    {
        lock (_gate)
        {
            _disposeTask ??= DisposeCoreAsync();
            return _disposeTask;
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (_audioSource is not null)
        {
            try
            {
                await _audioSource.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ein Fehler beim Freigeben der Audioquelle verhindert die weiteren Aufräumschritte nicht.
            }
            _audioSource = null;
        }

        _call?.Dispose();
        _call = null;

        _events.Complete();

        // Vor dem Freigeben der Abbruchquelle beide Schleifen beobachten: Auch beim direkten
        // Freigeben ohne vorherigen Stopp sollen ihre weitergeworfenen Fehler nicht unbeobachtet bleiben.
        // Der Stoppvorgang wartet bereits auf sie. Dieser Pfad übernimmt das beim direkten Freigeben.
        await GrpcDuplexStreamTeardown.ObserveAsync(_sendTask).ConfigureAwait(false);
        await GrpcDuplexStreamTeardown.ObserveAsync(_receiveTask).ConfigureAwait(false);

        _sessionCts.Dispose();
        _sendTask = null;
        _receiveTask = null;
        _stopTask = null;

        await _trace.DisposedAsync().ConfigureAwait(false);
    }
}
