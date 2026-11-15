using System.Collections.Concurrent;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class SynchronousClientMessageBus : IClientMessageBus, ISpeechInteractionSignals
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<object, ValueTask>>> _topics = new();
    private readonly ClientMessageBusSpeechInteractionSignals _signals;

    public SynchronousClientMessageBus()
    {
        _signals = new ClientMessageBusSpeechInteractionSignals(this);
    }

    public SpeechAssistantActivationState ActivationState => _signals.ActivationState;

    public bool IsRecording => _signals.IsRecording;

    public EarconFamily? ActiveFamily => _signals.ActiveFamily;

    public SpeechWakeListeningFallbackState WakeListeningFallbackState => _signals.WakeListeningFallbackState;

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull
    {
        if (!_topics.TryGetValue(typeof(TMessage), out var handlers))
        {
            return ValueTask.CompletedTask;
        }

        return PublishCoreAsync(message, handlers);
    }

    public IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler) where TMessage : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        var handlers = _topics.GetOrAdd(typeof(TMessage), static _ => new ConcurrentBag<Func<object, ValueTask>>());
        handlers.Add(msg => handler((TMessage)msg));
        return NoOpDisposable.Instance;
    }

    private static async ValueTask PublishCoreAsync(object message, ConcurrentBag<Func<object, ValueTask>> handlers)
    {
        foreach (var handler in handlers)
        {
            await handler(message);
        }
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



