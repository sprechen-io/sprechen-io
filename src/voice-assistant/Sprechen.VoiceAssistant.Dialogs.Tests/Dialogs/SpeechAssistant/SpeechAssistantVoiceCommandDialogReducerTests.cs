namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantVoiceCommandDialogReducerTests
{
    [Fact]
    public void Reduce_WhenLanguageChangedWithSelection_RestartsVoiceCommandWithNormalizedLanguage()
    {
        var state = CreateState() with
        {
            SelectedApplicationId = " app ",
            SelectedFormId = " form "
        };

        var update = SpeechAssistantVoiceCommandDialogReducer.Reduce(
            state,
            new SpeechAssistantVoiceCommandLanguageChangedEvent("de-DE"));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Equal("app", update.State.SelectedApplicationId);
        Assert.Equal("form", update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunVoiceCommandDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenLanguageChangedWithoutSelection_CompletesAndQueuesReturnToArmed()
    {
        var state = CreateState() with
        {
            SelectedApplicationId = null,
            SelectedFormId = "form"
        };

        var update = SpeechAssistantVoiceCommandDialogReducer.Reduce(
            state,
            new SpeechAssistantVoiceCommandLanguageChangedEvent("de"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Null(update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenLanguageChangeFailed_CompletesAndQueuesReturnToArmed()
    {
        var state = CreateState();

        var update = SpeechAssistantVoiceCommandDialogReducer.Reduce(
            state,
            new SpeechAssistantVoiceCommandLanguageChangeFailedEvent());

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Null(update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    private static SpeechAssistantState CreateState()
        => SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "en",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            CorrelationId = "corr"
        };
}
