using Sprechen.Speech.App.Contracts.Services;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Proto.Transport;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Koordiniert TTS-Anfrage, PCM-Wiedergabe und Abbruchsignale. Die eng begrenzte Wiederholung
/// bleibt auf Fehler vor dem ersten PCM-Einreihen beschränkt, damit bereits begonnenes Audio
/// nicht durch einen erneuten Versuch nochmals eingeplant wird.
/// </summary>
internal sealed partial class TextToSpeechPlaybackRunner
{
    // Die feste .NET-Frist signalisiert den Abbruch beim Warten auf das Ausspielen.
    // Der JavaScript-Wächter berechnet seine eigene Frist aus der ausstehenden Spieldauer.
    // Das Beenden des Wartevorgangs setzt voraus, dass der Player das Abbruchsignal beachtet.
    private static readonly TimeSpan DefaultDrainTimeout = TimeSpan.FromSeconds(60);
    private static readonly TextToSpeechPrePcmEnqueueRetryPolicy RetryPolicy = new();

    private readonly TimeSpan _firstPcmEnqueueTimeout;

    private readonly SemaphoreSlim _playbackGate = new(1, 1);
    private readonly SpeechGrpcClient _languageClient;
    private readonly TextToSpeechRequestFactory _requestFactory;
    private readonly TextToSpeechPcmStreamPlayer _pcmStreamPlayer;
    private readonly TextToSpeechEarconFeedback _earconFeedback;
    private readonly IBargeInCoordinator _bargeInCoordinator;
    private readonly ITextToSpeechPlaybackState _playbackState;
    private readonly TextToSpeechPlaybackCancellationState _playbackCancellation;
    private readonly ISpeechSessionIdentity? _sessionIdentity;
    private readonly ILogger<TextToSpeechPlaybackRunner> _logger;

    internal TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    internal TimeSpan DrainTimeout { get; init; } = DefaultDrainTimeout;

    public TextToSpeechPlaybackRunner(
        SpeechGrpcClient languageClient,
        TextToSpeechRequestFactory requestFactory,
        TextToSpeechPcmStreamPlayer pcmStreamPlayer,
        TextToSpeechEarconFeedback earconFeedback,
        IBargeInCoordinator bargeInCoordinator,
        ITextToSpeechPlaybackState playbackState,
        TextToSpeechPlaybackCancellationState playbackCancellation,
        TimeSpan firstPcmEnqueueTimeout,
        ISpeechSessionIdentity? sessionIdentity = null,
        ILogger<TextToSpeechPlaybackRunner>? logger = null)
    {
        if (firstPcmEnqueueTimeout <= TimeSpan.Zero || firstPcmEnqueueTimeout.TotalMilliseconds > uint.MaxValue - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(firstPcmEnqueueTimeout));
        }

        _firstPcmEnqueueTimeout = firstPcmEnqueueTimeout;
        _languageClient = languageClient ?? throw new ArgumentNullException(nameof(languageClient));
        _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
        _pcmStreamPlayer = pcmStreamPlayer ?? throw new ArgumentNullException(nameof(pcmStreamPlayer));
        _earconFeedback = earconFeedback ?? throw new ArgumentNullException(nameof(earconFeedback));
        _bargeInCoordinator = bargeInCoordinator ?? throw new ArgumentNullException(nameof(bargeInCoordinator));
        _playbackState = playbackState ?? throw new ArgumentNullException(nameof(playbackState));
        _playbackCancellation = playbackCancellation ?? throw new ArgumentNullException(nameof(playbackCancellation));
        // Die Identität bleibt für direkte Konstruktionen optional. Ohne sie wird keine
        // Sitzungskennung in die Anfrage geschrieben. Eine Zuordnung wird nicht erfunden.
        _sessionIdentity = sessionIdentity;
        _logger = logger ?? NullLogger<TextToSpeechPlaybackRunner>.Instance;
    }

    /// <summary>
    /// Erzeugt Metadaten zur Art der Sprachausgabe und ergänzt ihre Sitzungskennung, sofern vorhanden.
    /// </summary>
    /// <remarks>
    /// Die Zuordnung erfolgt über die Sitzungskennung, nicht über die Zeit.
    /// </remarks>
    private Metadata BuildHeaders(SpokenAnnouncementKind announcementKind)
    {
        var headers = SprechenGrpcAnnouncementMetadata.CreateHeaders(announcementKind.ToString());
        if (_sessionIdentity is not null)
        {
            SprechenGrpcSessionMetadata.AddOrReplaceSessionId(headers, _sessionIdentity.SessionId);
        }

        return headers;
    }

    public async Task SpeakAsync(
        string text,
        string languageCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        var request = _requestFactory.Create(text, languageCode, speechRate);
        var playbackCts = _playbackCancellation.Replace();

        try
        {
            await _playbackGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            using var intentionalCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, playbackCts.Token, _bargeInCoordinator.Token);
            var intentionalToken = intentionalCts.Token;
            bool timedOut;
            try
            {
                _playbackState.NotifyPlaybackStarted();
                timedOut = await RunPlaybackAsync(request, languageCode, announcementKind, intentionalToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _playbackState.NotifyPlaybackCompleted();
                _playbackGate.Release();
            }

            if (timedOut)
            {
                // Der Klang darf selbst Rückmeldungen auslösen, die eine neue Sprachausgabe
                // starten. Darum ist der Wiedergabeplatz vor diesem Aufruf bereits frei.
                intentionalToken.ThrowIfCancellationRequested();
                await _earconFeedback.PlayTimeoutBestEffortAsync(intentionalToken).ConfigureAwait(false);

                intentionalToken.ThrowIfCancellationRequested();
                throw new SpeechErrorException(
                    SpeechErrorCode.ProviderFailure,
                    "Text-to-speech timed out before the first PCM block was enqueued.");
            }
        }
        finally
        {
            _playbackCancellation.DisposeIfCurrent(playbackCts);
        }
    }

    private async Task<bool> RunPlaybackAsync(
        Sprechen.Proto.TextRequest request,
        string languageCode,
        SpokenAnnouncementKind announcementKind,
        CancellationToken intentionalToken)
    {
        // Nur einer gewinnt: erster erfolgreicher Enqueue oder Fristablauf.
        const int waiting = 0;
        const int begun = 1;
        const int expired = 2;
        var firstPcmState = waiting;
        using var budgetCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(intentionalToken, budgetCts.Token);
        var token = linkedCts.Token;
        await using var timer = TimeProvider.CreateTimer(_ =>
        {
            if (Interlocked.CompareExchange(ref firstPcmState, expired, waiting) == waiting)
            {
                budgetCts.Cancel();
            }
        }, null, _firstPcmEnqueueTimeout, Timeout.InfiniteTimeSpan);

        void OnFirstPcmEnqueued()
        {
            if (Interlocked.CompareExchange(ref firstPcmState, begun, waiting) == expired)
            {
                throw new OperationCanceledException(token);
            }
        }

        try
        {
            await _earconFeedback.StopBestEffortAsync(token).ConfigureAwait(false);
            var logicalPlaybackId = Guid.NewGuid();
            LogLogicalPlaybackStarted(logicalPlaybackId);

            await _pcmStreamPlayer.PrepareAsync(languageCode, token).ConfigureAwait(false);

            RpcException? lastTransportFailure = null;
            for (var physicalAttempt = 1; physicalAttempt <= 2; physicalAttempt++)
            {
                token.ThrowIfCancellationRequested();
                var enqueueProgress = new TextToSpeechPcmEnqueueProgressState();
                LogPhysicalAttemptStarted(logicalPlaybackId, physicalAttempt);
                var call = _languageClient.TextToSpeechToStream(
                    request,
                    headers: BuildHeaders(announcementKind),
                    cancellationToken: token);
                try
                {
                    using (call)
                    {
                        var chunkCount = await _pcmStreamPlayer
                            .PlayAsync(call, enqueueProgress, OnFirstPcmEnqueued, token)
                            .ConfigureAwait(false);
                        if (chunkCount == 0)
                        {
                            throw new InvalidOperationException("Text-to-speech returned no audio.");
                        }

                        using var drainTimeoutCts = new CancellationTokenSource(DrainTimeout);
                        using var drainCts = CancellationTokenSource.CreateLinkedTokenSource(token, drainTimeoutCts.Token);
                        try
                        {
                            await _pcmStreamPlayer.DrainAsync(
                                Volatile.Read(ref firstPcmState) == waiting ? OnFirstPcmEnqueued : null,
                                drainCts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (
                            drainTimeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
                        {
                            throw new SpeechErrorException(
                                SpeechErrorCode.ProviderFailure,
                                "Text-to-speech playback timed out while draining audio.");
                        }

                        LogLogicalPlaybackCompleted(logicalPlaybackId, physicalAttempt);
                        return false;
                    }
                }
                catch (RpcException ex)
                {
                    intentionalToken.ThrowIfCancellationRequested();
                    if (Volatile.Read(ref firstPcmState) == expired)
                    {
                        throw new OperationCanceledException(token);
                    }

                    lastTransportFailure = ex;
                    LogPhysicalAttemptFailed(logicalPlaybackId, physicalAttempt, ex.StatusCode.ToString(), enqueueProgress.State.ToString());
                    var failure = new TextToSpeechAttemptFailure(
                        physicalAttempt,
                        ex.StatusCode,
                        ex.Status.Detail,
                        enqueueProgress.State,
                        intentionalToken.IsCancellationRequested,
                        false,
                        false);
                    if (RetryPolicy.Evaluate(failure) != TextToSpeechRetryEligibility.Eligible)
                    {
                        await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
                        LogLogicalPlaybackFailed(logicalPlaybackId, physicalAttempt, enqueueProgress.State.ToString());
                        throw SpeechErrorExceptionFactory.Create(ex, intentionalToken);
                    }

                    token.ThrowIfCancellationRequested();
                    await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    await _pcmStreamPlayer.PrepareAsync(languageCode, token).ConfigureAwait(false);
                }
            }

            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            LogLogicalPlaybackFailed(logicalPlaybackId, physicalAttempt: 2, TextToSpeechPcmEnqueueState.NotStarted.ToString());
            if (lastTransportFailure is not null)
            {
                throw SpeechErrorExceptionFactory.Create(lastTransportFailure, intentionalToken);
            }

            throw new InvalidOperationException("Text-to-speech physical retry budget exhausted.");
        }
        catch (Exception) when (intentionalToken.IsCancellationRequested)
        {
            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            throw new OperationCanceledException(intentionalToken);
        }
        catch (Exception) when (Volatile.Read(ref firstPcmState) == expired)
        {
            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            return true;
        }
        catch (RpcException ex)
        {
            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            throw SpeechErrorExceptionFactory.Create(ex, intentionalToken);
        }
        catch (OperationCanceledException)
        {
            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            throw;
        }
        catch
        {
            // Ein Playerfehler kann auftreten, nachdem frühere Blöcke bereits eingeplant wurden.
            // Diese Quellen zurücksetzen, bevor die Abschlussmeldung im finally die Aufnahme wieder freigibt.
            await _pcmStreamPlayer.ResetBestEffortAsync().ConfigureAwait(false);
            throw;
        }
    }

    [LoggerMessage(EventId = 4001, EventName = "TextToSpeechLogicalPlaybackStarted", Level = LogLevel.Information, Message = "TextToSpeechLogicalPlaybackStarted {LogicalPlaybackId}")]
    private partial void LogLogicalPlaybackStarted(Guid logicalPlaybackId);

    [LoggerMessage(EventId = 4002, EventName = "TextToSpeechPhysicalAttemptStarted", Level = LogLevel.Information, Message = "TextToSpeechPhysicalAttemptStarted {LogicalPlaybackId} {PhysicalAttempt}")]
    private partial void LogPhysicalAttemptStarted(Guid logicalPlaybackId, int physicalAttempt);

    [LoggerMessage(EventId = 4003, EventName = "TextToSpeechPhysicalAttemptFailed", Level = LogLevel.Warning, Message = "TextToSpeechPhysicalAttemptFailed {LogicalPlaybackId} {PhysicalAttempt} {Status} {EnqueueState}")]
    private partial void LogPhysicalAttemptFailed(Guid logicalPlaybackId, int physicalAttempt, string status, string enqueueState);

    [LoggerMessage(EventId = 4004, EventName = "TextToSpeechLogicalPlaybackCompleted", Level = LogLevel.Information, Message = "TextToSpeechLogicalPlaybackCompleted {LogicalPlaybackId} {PhysicalAttempt}")]
    private partial void LogLogicalPlaybackCompleted(Guid logicalPlaybackId, int physicalAttempt);

    [LoggerMessage(EventId = 4005, EventName = "TextToSpeechLogicalPlaybackFailed", Level = LogLevel.Warning, Message = "TextToSpeechLogicalPlaybackFailed {LogicalPlaybackId} {PhysicalAttempt} {EnqueueState}")]
    private partial void LogLogicalPlaybackFailed(Guid logicalPlaybackId, int physicalAttempt, string enqueueState);
}
