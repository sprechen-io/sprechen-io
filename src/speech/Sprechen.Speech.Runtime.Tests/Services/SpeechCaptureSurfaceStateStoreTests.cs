using Microsoft.Extensions.Logging.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSurfaceStateStoreTests
{
    [Fact]
    public async Task Current_WhenSignalsChange_MapsActivationRecordingAndEarconState()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        using var store = new SpeechCaptureSurfaceStateStore(signals, signals, signals, signals);

        await signals.SetActivationStateAsync(SpeechAssistantActivationState.Ready);
        await using var recordingLease = await signals.BeginRecordingAsync();
        await signals.SetActiveFamilyAsync(EarconFamily.Structure);
        await signals.SetWakeListeningFallbackStateAsync(new SpeechWakeListeningFallbackState(
            true,
            "WAKE_DETECTOR_UNAVAILABLE",
            LexicalWakeFallbackReason.DetectorUnavailable));

        Assert.Equal(SpeechAssistantActivationState.Ready, store.Current.ActivationState);
        Assert.True(store.Current.IsRecording);
        Assert.Equal(EarconFamily.Structure, store.Current.ActiveEarconFamily);
        Assert.True(store.Current.WakeListeningFallbackState.IsLexicalFallbackActive);
    }

    [Fact]
    public async Task Changed_WhenSignalsPublishDistinctState_RaisesChangedEvent()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        using var store = new SpeechCaptureSurfaceStateStore(signals, signals, signals, signals);
        var changeCount = 0;

        store.Changed += () => changeCount++;

        await signals.SetActivationStateAsync(SpeechAssistantActivationState.Starting);
        await signals.SetActivationStateAsync(SpeechAssistantActivationState.Ready);

        Assert.Equal(2, changeCount);
    }
}
