using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.App.Contracts.Services;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Verbindet lokale Audioaufnahme und gRPC-Sende- und Empfangsschleifen zur
/// Sprachbestätigung. Ein Ergebnis entsteht beim Stoppen nur mit vorhandenem Transkript.
/// </summary>
internal sealed class LanguageConfirmationStreamingSession : ILanguageConfirmationStreamingSession
{
    private static readonly TimeSpan DefaultStopGrace = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private readonly CancellationTokenSource _sessionCts;
    private readonly LanguageConfirmationConversationState _conversationState;
    private readonly GrpcDuplexStreamTeardown _teardown;

    private ISpeechAudioChunkSource? _audioSource;
    private AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>? _speechCall;
    private Task? _sendTask;
    private Task? _receiveTask;
    private Task? _stopTask;
    private Task? _disposeTask;
    private Task<LanguageConfirmationStreamingResult>? _resultTask;
    private bool _disposed;

    public LanguageConfirmationStreamingSession(
        ISpeechAudioChunkSource audioSource,
        AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent> speechCall,
        string? languageCode,
        SupportedLanguages supportedLanguages,
        CancellationToken cancellationToken)
        : this(audioSource, speechCall, languageCode, supportedLanguages, DefaultStopGrace, cancellationToken)
    {
    }

    public LanguageConfirmationStreamingSession(
        ISpeechAudioChunkSource audioSource,
        AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent> speechCall,
        string? languageCode,
        SupportedLanguages supportedLanguages,
        TimeSpan stopGrace,
        CancellationToken cancellationToken)
    {
        _teardown = new GrpcDuplexStreamTeardown(stopGrace);
        _audioSource = audioSource ?? throw new ArgumentNullException(nameof(audioSource));
        _speechCall = speechCall ?? throw new ArgumentNullException(nameof(speechCall));

        _conversationState = new LanguageConfirmationConversationState(supportedLanguages, languageCode);
        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _sendTask = new GrpcAudioChunkSendLoop().WriteAsync(
            _audioSource.Chunks,
            _speechCall.RequestStream,
            _audioSource.MimeType,
            _sessionCts.Token,
            languageCode);
        _receiveTask = new LanguageConfirmationStreamingReceiveLoop(_conversationState, _sessionCts.Token)
            .RunAsync(_speechCall.ResponseStream, _sessionCts.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Task stopTask;
        lock (_gate)
        {
            ThrowIfDisposed();
            stopTask = _stopTask ??= StopCoreAsync();
        }

        return stopTask.WaitAsync(cancellationToken);
    }

    public Task<LanguageConfirmationStreamingResult> GetResultAsync(CancellationToken cancellationToken)
    {
        Task<LanguageConfirmationStreamingResult> resultTask;
        lock (_gate)
        {
            ThrowIfDisposed();
            resultTask = _resultTask ?? throw new InvalidOperationException("StopAsync must be called before GetResultAsync.");
        }

        return resultTask.WaitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(DisposeOnceAsync());
    }

    private Task DisposeOnceAsync()
    {
        lock (_gate)
        {
            if (_disposeTask is not null)
            {
                return _disposeTask;
            }

            _disposed = true;
            var stopTask = _stopTask;
            _disposeTask = stopTask is null ? CancelAndDisposeAsync() : DisposeAfterStopAsync(stopTask);
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

        _speechCall?.Dispose();
        _speechCall = null;

        // Vor dem Freigeben der Abbruchquelle beide Schleifen beobachten: Auch beim direkten
        // Freigeben ohne vorherigen Stopp sollen ihre weitergeworfenen Fehler nicht unbeobachtet bleiben.
        await GrpcDuplexStreamTeardown.ObserveAsync(_sendTask).ConfigureAwait(false);
        await GrpcDuplexStreamTeardown.ObserveAsync(_receiveTask).ConfigureAwait(false);

        _sendTask = null;
        _receiveTask = null;
        _stopTask = null;
        _resultTask = null;
        _sessionCts.Dispose();
    }

    private async Task CancelAndDisposeAsync()
    {
        _sessionCts.Cancel();
        await DisposeCoreAsync().ConfigureAwait(false);
    }

    private async Task DisposeAfterStopAsync(Task stopTask)
    {
        await _teardown.AwaitInFlightStopWithGraceAsync(stopTask, _sessionCts).ConfigureAwait(false);
        await DisposeCoreAsync().ConfigureAwait(false);
    }

    private async Task StopCoreAsync()
    {
        var audioSource = _audioSource;

        try
        {
            if (audioSource is not null)
            {
                await audioSource.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }

            // Wie bei der Transkriptionssitzung folgt nach der Schonfrist ein Abbruchsignal.
            // Anschließend wird weiter auf die Schleifen gewartet. Sie müssen auf den Abbruch reagieren.
            await _teardown.AwaitSendReceiveWithGraceAsync(_sendTask, _receiveTask, _sessionCts).ConfigureAwait(false);

            _conversationState.EnsureTranscriptPresent();

            lock (_gate)
            {
                _resultTask ??= Task.FromResult(_conversationState.BuildResult());
            }
        }
        catch (RpcException ex)
        {
            throw SpeechErrorExceptionFactory.Create(ex, _sessionCts.Token);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LanguageConfirmationStreamingSession));
        }
    }
}
