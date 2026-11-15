namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantFormSelectionReducerTests
{
    [Fact]
    public void Reduce_WhenFormSelected_StartsVoiceCommandDialog()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormSelection,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            CorrelationId = "corr"
        };

        var update = SpeechAssistantFormSelectionReducer.Reduce(
            state,
            new SpeechAssistantFormSelectedEvent(FormId.From("form"), "Form"));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Equal("form", update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunVoiceCommandDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenSelectionCancelled_ReturnsToFormsCatalogAndClearsSelection()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormSelection,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
            CorrelationId = "corr"
        };

        var update = SpeechAssistantFormSelectionReducer.Reduce(state, new SpeechAssistantFormSelectionCancelledEvent());

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.Null(update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunFormsCatalogDialogEffect>(effect));
    }
}
