namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantReducerTransitionsTests
{
    [Fact]
    public void CompleteAndQueueReturnToArmedWithResolvedLanguage_NormalizesLanguageAndQueuesReturn()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Armed,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            SelectedIsoCode = "en-US"
        };

        var update = SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithResolvedLanguage(state);

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("en", update.State.SelectedIsoCode);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void CompleteAndQueueReturnToArmedWithoutSelection_ClearsSelectionAndQueuesReturn()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedApplicationId = "app",
            SelectedFormId = "form"
        };

        var update = SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithoutSelection(state);

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Null(update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void UnsupportedEvent_WhenDialogIsProvided_NamesPhaseAndDialog()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SpeechAssistantReducerTransitions.UnsupportedEvent(
                new SpeechAssistantPressedEvent(),
                SpeechAssistantPhase.Engaged,
                SpeechAssistantDialogKind.VoiceCommand));

        Assert.Contains("Unsupported speech assistant event", exception.Message, StringComparison.Ordinal);
        Assert.Contains("SpeechAssistantPressedEvent", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Engaged", exception.Message, StringComparison.Ordinal);
        Assert.Contains("VoiceCommand", exception.Message, StringComparison.Ordinal);
    }
}
