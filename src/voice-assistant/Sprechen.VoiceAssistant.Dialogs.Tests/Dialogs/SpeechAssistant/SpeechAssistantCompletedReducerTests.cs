namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantCompletedReducerTests
{
    [Fact]
    public void Reduce_WhenReturnToArmed_PlaysListeningCueBeforeWakeListening()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Completed,
            SelectedIsoCode = "de",
            CorrelationId = "corr"
        };

        var update = SpeechAssistantCompletedReducer.Reduce(state, new SpeechAssistantReturnToArmedEvent());

        Assert.Equal(SpeechAssistantPhase.Armed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Null(update.State.CorrelationId);
        Assert.Collection(
            update.Effects,
            effect => Assert.IsType<PlayArmedListeningCueEffect>(effect),
            effect => Assert.IsType<ListenForWakeWordEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenLanguageSelected_StaysCompletedAndDispatchesReturnToArmed()
    {
        var state = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "en" };

        var update = SpeechAssistantCompletedReducer.Reduce(state, new SpeechAssistantLanguageSelectedEvent("de-DE"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }
}
