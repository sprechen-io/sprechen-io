using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Beobachtet Sprachausgabe und Earcons gemeinsam, damit überlappende Wiedergabe
/// die Mikrofonfreigabe nicht verfrüht auslöst. Bei HardOff wird zu Wiedergabebeginn
/// die Sperre angefordert und erst nach Ende beider Quellen wieder geöffnet.
/// </summary>
internal sealed class MicGateLifecycleObserver : IMicGateState, IDisposable
{
    private readonly ITextToSpeechPlaybackState _ttsState;
    private readonly IEarconPlaybackState _earconState;
    private readonly IMicGatePolicyResolver _policyResolver;
    private readonly IMicGateBrowserBridge _browserBridge;
    private readonly ILogger<MicGateLifecycleObserver> _logger;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _disposeCancellation = new();
    private readonly Action _handler;
    private bool _isAnyPlaying;
    private bool _isHardOff;
    private long _desiredVersion;
    private bool _disposed;
    // Wer während laufender Wiedergabe aufnehmen will, wartet hier. Erst deren Ende
    // und die bestätigte Browseröffnung schließen das Warten erfolgreich ab. Fehler der Sperrsteuerung werden weitergegeben.
    private TaskCompletionSource? _openAgain;

    public MicGateLifecycleObserver(
        ITextToSpeechPlaybackState ttsState,
        IEarconPlaybackState earconState,
        IMicGatePolicyResolver policyResolver,
        IMicGateBrowserBridge browserBridge,
        ILogger<MicGateLifecycleObserver> logger)
    {
        _ttsState = ttsState ?? throw new ArgumentNullException(nameof(ttsState));
        _earconState = earconState ?? throw new ArgumentNullException(nameof(earconState));
        _policyResolver = policyResolver ?? throw new ArgumentNullException(nameof(policyResolver));
        _browserBridge = browserBridge ?? throw new ArgumentNullException(nameof(browserBridge));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _handler = OnPlaybackStateChanged;
        _ttsState.PlaybackStateChanged += _handler;
        _earconState.PlaybackStateChanged += _handler;
    }

    public bool IsHardOff
    {
        get
        {
            lock (_gate)
            {
                return _isHardOff;
            }
        }
    }

    public ValueTask WaitUntilOpenAsync(CancellationToken cancellationToken)
    {
        Task pending;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_isHardOff)
            {
                return ValueTask.CompletedTask;
            }

            _openAgain ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            pending = _openAgain.Task;
        }

        return new ValueTask(pending.WaitAsync(cancellationToken));
    }

    private void OnPlaybackStateChanged()
    {
        bool? toggleTo = null;
        long desiredVersion = 0;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var nextAnyPlaying = _ttsState.IsPlaying || _earconState.IsPlaying;
            var policy = _policyResolver.GetPolicy();

            if (nextAnyPlaying && !_isAnyPlaying)
            {
                if (policy == MicGatePolicy.HardOff)
                {
                    _isHardOff = true;
                    toggleTo = false;
                    desiredVersion = ++_desiredVersion;
                }
            }
            else if (!nextAnyPlaying && _isAnyPlaying)
            {
                if (_isHardOff)
                {
                    toggleTo = true;
                    desiredVersion = ++_desiredVersion;
                }
            }

            _isAnyPlaying = nextAnyPlaying;
        }

        if (toggleTo is { } enabled)
        {
            _ = PublishDesiredStateAsync(enabled, desiredVersion);
        }
    }

    private async Task PublishDesiredStateAsync(bool enabled, long desiredVersion)
    {
        try
        {
            Task publication;
            lock (_gate)
            {
                if (_disposed || desiredVersion != _desiredVersion)
                {
                    return;
                }

                publication = _browserBridge.SetMicGateAsync(enabled, _disposeCancellation.Token);
            }

            await publication.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MicGate browser bridge call failed (enabled={Enabled}).", enabled);
            TaskCompletionSource? failedWaiters = null;
            lock (_gate)
            {
                if (!_disposed
                    && !enabled
                    && _isHardOff)
                {
                    // Ein fehlgeschlagenes Schließen kann die Sitzung vor der Aufnahmebereitschaft
                    // freigeben. Wartende hinter HardOff müssen diesen Abschlussfehler behalten.
                    // Sonst wirkt das spätere Öffnen ohne Sitzung erfolgreich und sie beginnen
                    // eine Aufnahme, die bereits beendet wurde.
                    _openAgain ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    failedWaiters = _openAgain;
                }
                else if (desiredVersion == _desiredVersion
                    && enabled
                    && !_isAnyPlaying
                    && _isHardOff)
                {
                    _isHardOff = false;
                    failedWaiters = _openAgain;
                    _openAgain = null;
                }
            }

            failedWaiters?.TrySetException(ex);
            return;
        }

        TaskCompletionSource? releaseWaiters = null;
        lock (_gate)
        {
            if (desiredVersion == _desiredVersion
                && enabled
                && !_isAnyPlaying
                && _isHardOff)
            {
                _isHardOff = false;
                releaseWaiters = _openAgain;
                _openAgain = null;
            }
        }

        releaseWaiters?.TrySetResult();
    }

    public void Dispose()
    {
        TaskCompletionSource? disposedWaiters;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _desiredVersion++;
            _isAnyPlaying = false;
            _isHardOff = false;
            disposedWaiters = _openAgain;
            _openAgain = null;
        }

        _ttsState.PlaybackStateChanged -= _handler;
        _earconState.PlaybackStateChanged -= _handler;
        _disposeCancellation.Cancel();
        disposedWaiters?.TrySetException(new ObjectDisposedException(nameof(MicGateLifecycleObserver)));
    }
}
