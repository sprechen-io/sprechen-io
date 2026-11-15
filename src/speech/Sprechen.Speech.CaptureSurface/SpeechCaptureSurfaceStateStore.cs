using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.CaptureSurface;

/// <summary>
/// Führt Aktivierung, Aufnahme, Earcon-Familie und lexikalischen Ersatzweg im Oberflächenzustand zusammen,
/// damit Beobachter Änderungen über einen gemeinsamen Snapshot erhalten.
/// </summary>
public sealed class SpeechCaptureSurfaceStateStore :
    ISpeechCaptureSurfaceState,
    IDisposable
{
    private readonly object _gate = new();
    private readonly IDisposable _activationSubscription;
    private readonly IDisposable _recordingSubscription;
    private readonly IDisposable _earconSubscription;
    private readonly IDisposable _wakeListeningFallbackSubscription;
    private SpeechCaptureSurfaceStateSnapshot _current;
    private bool _disposed;

    public SpeechCaptureSurfaceStateStore(
        ISpeechCaptureFeedback speechCaptureFeedback,
        ISpeechCaptureRecordingIndicator recordingIndicator,
        IEarconFamilySignals earconFamilySignals,
        ISpeechWakeListeningFallbackSignals wakeListeningFallbackSignals)
    {
        ArgumentNullException.ThrowIfNull(speechCaptureFeedback);
        ArgumentNullException.ThrowIfNull(recordingIndicator);
        ArgumentNullException.ThrowIfNull(earconFamilySignals);
        ArgumentNullException.ThrowIfNull(wakeListeningFallbackSignals);

        _current = new SpeechCaptureSurfaceStateSnapshot(
            speechCaptureFeedback.ActivationState,
            recordingIndicator.IsRecording,
            earconFamilySignals.ActiveFamily,
            wakeListeningFallbackSignals.WakeListeningFallbackState);
        _activationSubscription = speechCaptureFeedback.SubscribeToActivationStateChanged(OnActivationStateChangedAsync);
        _recordingSubscription = recordingIndicator.SubscribeToRecordingStateChanged(OnRecordingStateChangedAsync);
        _earconSubscription = earconFamilySignals.SubscribeToActiveFamilyChanged(OnEarconFamilyChangedAsync);
        _wakeListeningFallbackSubscription = wakeListeningFallbackSignals.SubscribeToWakeListeningFallbackStateChanged(OnWakeListeningFallbackChangedAsync);
    }

    public event Action? Changed;

    public SpeechCaptureSurfaceStateSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activationSubscription.Dispose();
        _recordingSubscription.Dispose();
        _earconSubscription.Dispose();
        _wakeListeningFallbackSubscription.Dispose();
    }

    private ValueTask OnActivationStateChangedAsync(SpeechAssistantActivationStateChanged message)
    {
        UpdateCurrent(snapshot => snapshot with { ActivationState = message.State });
        return ValueTask.CompletedTask;
    }

    private ValueTask OnRecordingStateChangedAsync(SpeechAssistantRecordingStateChanged message)
    {
        UpdateCurrent(snapshot => snapshot with { IsRecording = message.IsRecording });
        return ValueTask.CompletedTask;
    }

    private ValueTask OnEarconFamilyChangedAsync(EarconFamilyChanged message)
    {
        UpdateCurrent(snapshot => snapshot with { ActiveEarconFamily = message.ActiveFamily });
        return ValueTask.CompletedTask;
    }

    private ValueTask OnWakeListeningFallbackChangedAsync(SpeechWakeListeningFallbackStateChanged message)
    {
        UpdateCurrent(snapshot => snapshot with
        {
            WakeListeningFallbackState = new SpeechWakeListeningFallbackState(
                message.IsLexicalFallbackActive,
                message.ReasonCode,
                message.Reason)
        });

        return ValueTask.CompletedTask;
    }

    private void UpdateCurrent(
        Func<SpeechCaptureSurfaceStateSnapshot, SpeechCaptureSurfaceStateSnapshot> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var changed = false;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var next = update(_current);
            if (next == _current)
            {
                return;
            }

            _current = next;
            changed = true;
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

}
