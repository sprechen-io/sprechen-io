namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpClientMessageBus : IClientMessageBus, ISpeechInteractionSignals
{
    private readonly ClientMessageBusSpeechInteractionSignals _signals;

    public NoOpClientMessageBus()
    {
        _signals = new ClientMessageBusSpeechInteractionSignals(this);
    }

    public SpeechAssistantActivationState ActivationState => _signals.ActivationState;

    public bool IsRecording => _signals.IsRecording;

    public EarconFamily? ActiveFamily => _signals.ActiveFamily;

    public SpeechWakeListeningFallbackState WakeListeningFallbackState => _signals.WakeListeningFallbackState;

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull => ValueTask.CompletedTask;

    public IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler) where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NoOpDisposable.Instance;
    }

    public ValueTask SetActivationStateAsync(SpeechAssistantActivationState activationState)
        => _signals.SetActivationStateAsync(activationState);

    public ValueTask ReportFailureAsync(string message)
        => _signals.ReportFailureAsync(message);

    public ValueTask ReportFailureAsync(SpeechErrorCode errorCode, string message)
        => _signals.ReportFailureAsync(errorCode, message);

    public IDisposable SubscribeToActivationStateChanged(Func<SpeechAssistantActivationStateChanged, ValueTask> handler)
        => _signals.SubscribeToActivationStateChanged(handler);

    public IDisposable SubscribeToFailures(Func<SpeechCaptureFailed, ValueTask> handler)
        => _signals.SubscribeToFailures(handler);

    public ValueTask<IAsyncDisposable> BeginRecordingAsync()
        => _signals.BeginRecordingAsync();

    public ValueTask EnsureStoppedAsync()
        => _signals.EnsureStoppedAsync();

    public IDisposable SubscribeToRecordingStateChanged(Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
        => _signals.SubscribeToRecordingStateChanged(handler);

    public ValueTask PublishBargeInAsync()
        => _signals.PublishBargeInAsync();

    public IDisposable SubscribeToBargeIn(Func<ValueTask> handler)
        => _signals.SubscribeToBargeIn(handler);

    public ValueTask SetActiveFamilyAsync(EarconFamily? activeFamily)
        => _signals.SetActiveFamilyAsync(activeFamily);

    public IDisposable SubscribeToActiveFamilyChanged(Func<EarconFamilyChanged, ValueTask> handler)
        => _signals.SubscribeToActiveFamilyChanged(handler);

    public ValueTask SetWakeListeningFallbackStateAsync(SpeechWakeListeningFallbackState state)
        => _signals.SetWakeListeningFallbackStateAsync(state);

    public IDisposable SubscribeToWakeListeningFallbackStateChanged(
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler)
        => _signals.SubscribeToWakeListeningFallbackStateChanged(handler);

    public ValueTask PublishStoppedAsync(string languageCode, string? finalTranscript)
        => _signals.PublishStoppedAsync(languageCode, finalTranscript);

    public IDisposable SubscribeToStopped(Func<SpeechCaptureStopped, ValueTask> handler)
        => _signals.SubscribeToStopped(handler);
}


