using Sprechen.Speech.App.Contracts.Messages;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Bietet Clientaufrufern einen gemeinsamen Signalzugang. Die einzelnen Signalobjekte behalten die Verantwortung für ihre Zustände und Veröffentlichungen.
/// </summary>
public sealed class ClientMessageBusSpeechInteractionSignals(IClientMessageBus messageBus) : ISpeechInteractionSignals
{
    private readonly ClientMessageBusSpeechCaptureFeedbackSignals _captureFeedback =
        new(messageBus ?? throw new ArgumentNullException(nameof(messageBus)));
    private readonly ClientMessageBusSpeechRecordingSignals _recording = new(messageBus);
    private readonly ClientMessageBusWakeListeningFallbackSignals _wakeListeningFallback = new(messageBus);
    private readonly ClientMessageBusEarconFamilySignals _earconFamily = new(messageBus);
    private readonly ClientMessageBusCaptureStoppedSignals _captureStopped = new(messageBus);
    private readonly ClientMessageBusBargeInSignals _bargeIn = new(messageBus);

    public SpeechAssistantActivationState ActivationState => _captureFeedback.ActivationState;

    public bool IsRecording => _recording.IsRecording;

    public EarconFamily? ActiveFamily => _earconFamily.ActiveFamily;

    public SpeechWakeListeningFallbackState WakeListeningFallbackState => _wakeListeningFallback.WakeListeningFallbackState;

    ValueTask ISpeechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState activationState)
        => _captureFeedback.SetActivationStateAsync(activationState);

    ValueTask ISpeechCaptureFeedback.ReportFailureAsync(string message)
        => _captureFeedback.ReportFailureAsync(message);

    ValueTask ISpeechCaptureFeedback.ReportFailureAsync(SpeechErrorCode errorCode, string message)
        => _captureFeedback.ReportFailureAsync(errorCode, message);

    IDisposable ISpeechCaptureFeedback.SubscribeToActivationStateChanged(
        Func<SpeechAssistantActivationStateChanged, ValueTask> handler)
        => _captureFeedback.SubscribeToActivationStateChanged(handler);

    IDisposable ISpeechCaptureFeedback.SubscribeToFailures(Func<SpeechCaptureFailed, ValueTask> handler)
        => _captureFeedback.SubscribeToFailures(handler);

    ValueTask<IAsyncDisposable> ISpeechCaptureRecordingIndicator.BeginRecordingAsync()
        => _recording.BeginRecordingAsync();

    ValueTask ISpeechCaptureRecordingIndicator.EnsureStoppedAsync()
        => _recording.EnsureStoppedAsync();

    IDisposable ISpeechCaptureRecordingIndicator.SubscribeToRecordingStateChanged(
        Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
        => _recording.SubscribeToRecordingStateChanged(handler);

    ValueTask ISpeechWakeListeningFallbackSignals.SetWakeListeningFallbackStateAsync(
        SpeechWakeListeningFallbackState state)
        => _wakeListeningFallback.SetWakeListeningFallbackStateAsync(state);

    IDisposable ISpeechWakeListeningFallbackSignals.SubscribeToWakeListeningFallbackStateChanged(
        Func<SpeechWakeListeningFallbackStateChanged, ValueTask> handler)
        => _wakeListeningFallback.SubscribeToWakeListeningFallbackStateChanged(handler);

    ValueTask IEarconFamilySignals.SetActiveFamilyAsync(EarconFamily? activeFamily)
        => _earconFamily.SetActiveFamilyAsync(activeFamily);

    IDisposable IEarconFamilySignals.SubscribeToActiveFamilyChanged(Func<EarconFamilyChanged, ValueTask> handler)
        => _earconFamily.SubscribeToActiveFamilyChanged(handler);

    ValueTask ISpeechCaptureStoppedSignals.PublishStoppedAsync(string languageCode, string? finalTranscript)
        => _captureStopped.PublishStoppedAsync(languageCode, finalTranscript);

    IDisposable ISpeechCaptureStoppedSignals.SubscribeToStopped(Func<SpeechCaptureStopped, ValueTask> handler)
        => _captureStopped.SubscribeToStopped(handler);

    ValueTask ISpeechBargeInSignals.PublishBargeInAsync()
        => _bargeIn.PublishBargeInAsync();

    IDisposable ISpeechBargeInSignals.SubscribeToBargeIn(Func<ValueTask> handler)
        => _bargeIn.SubscribeToBargeIn(handler);
}
