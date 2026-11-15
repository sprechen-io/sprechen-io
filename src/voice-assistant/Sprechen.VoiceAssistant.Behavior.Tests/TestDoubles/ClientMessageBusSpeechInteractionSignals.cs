using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class ClientMessageBusSpeechInteractionSignals(ClientMessageBus messageBus) : ISpeechInteractionSignals
{
    private readonly ConcurrentQueue<SpeechCaptureFailed> _failures = new();
    private readonly ConcurrentBag<Func<SpeechCaptureFailed, ValueTask>> _failureHandlers = new();
    private bool _isRecording;
    private SpeechAssistantActivationState _activationState = SpeechAssistantActivationState.NotStarted;
    private SpeechWakeListeningFallbackState _wakeListeningFallbackState = SpeechWakeListeningFallbackState.Inactive;

    public SpeechAssistantActivationState ActivationState => _activationState;

    public bool IsRecording => _isRecording;

    public SpeechWakeListeningFallbackState WakeListeningFallbackState => _wakeListeningFallbackState;

    public EarconFamily? ActiveFamily { get; private set; }

    public IReadOnlyCollection<SpeechCaptureFailed> Failures => _failures.ToArray();

    public ValueTask SetActivationStateAsync(SpeechAssistantActivationState activationState)
    {
        _activationState = activationState;
        return ValueTask.CompletedTask;
    }

    public ValueTask ReportFailureAsync(string message)
    {
        return ReportFailureAsync(SpeechErrorCode.Unspecified, message);
    }

    public async ValueTask ReportFailureAsync(SpeechErrorCode errorCode, string message)
    {
        var failure = new SpeechCaptureFailed(errorCode, message);
        _failures.Enqueue(failure);
        messageBus.Logger.LogWarning("Speech capture failure {ErrorCode}: {Message}", errorCode, message);
        foreach (var handler in _failureHandlers)
        {
            await handler(failure).ConfigureAwait(false);
        }
    }

    public IDisposable SubscribeToActivationStateChanged(Func<SpeechAssistantActivationStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public IDisposable SubscribeToFailures(Func<SpeechCaptureFailed, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _failureHandlers.Add(handler);
        return NullScope.Instance;
    }

    public ValueTask<IAsyncDisposable> BeginRecordingAsync()
    {
        _isRecording = true;
        return ValueTask.FromResult<IAsyncDisposable>(new RecordingSpeechInteractionScope(this));
    }

    public ValueTask EnsureStoppedAsync()
    {
        StopRecording();
        return ValueTask.CompletedTask;
    }

    public IDisposable SubscribeToRecordingStateChanged(Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public ValueTask PublishBargeInAsync()
    {
        return ValueTask.CompletedTask;
    }

    public IDisposable SubscribeToBargeIn(Func<ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public ValueTask SetActiveFamilyAsync(EarconFamily? activeFamily)
    {
        ActiveFamily = activeFamily;
        return ValueTask.CompletedTask;
    }

    public IDisposable SubscribeToActiveFamilyChanged(Func<EarconFamilyChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public ValueTask SetWakeListeningFallbackStateAsync(SpeechWakeListeningFallbackState state)
    {
        _wakeListeningFallbackState = state;
        return ValueTask.CompletedTask;
    }

    public IDisposable SubscribeToWakeListeningFallbackStateChanged(
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public ValueTask PublishStoppedAsync(string languageCode, string? finalTranscript)
    {
        return ValueTask.CompletedTask;
    }

    public IDisposable SubscribeToStopped(Func<SpeechCaptureStopped, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return NullScope.Instance;
    }

    public void StopRecording()
    {
        _isRecording = false;
    }
}
