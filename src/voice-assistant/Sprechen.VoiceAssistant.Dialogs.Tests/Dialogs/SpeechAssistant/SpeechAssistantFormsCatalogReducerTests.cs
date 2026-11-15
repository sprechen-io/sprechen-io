namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantFormsCatalogReducerTests
{
    [Fact]
    public void Reduce_WhenApplicationHasMultipleForms_StartsFormSelectionWithEffectiveIsoCode()
    {
        var state = CreateFormsCatalogState() with
        {
            SelectedApplicationId = "old-app",
            SelectedFormId = "old-form"
        };

        var update = SpeechAssistantFormsCatalogReducer.Reduce(
            state,
            new SpeechAssistantApplicationSelectedEvent("new-app", "New App", 2, null, "en"));

        Assert.Equal(SpeechAssistantDialogKind.FormSelection, update.State.ActiveDialog);
        Assert.Equal("en", update.State.SelectedIsoCode);
        Assert.Equal("new-app", update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunFormSelectionDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenApplicationHasOneForm_StartsVoiceCommandWithDefaultForm()
    {
        var state = CreateFormsCatalogState();

        var update = SpeechAssistantFormsCatalogReducer.Reduce(
            state,
            new SpeechAssistantApplicationSelectedEvent("new-app", "New App", 1, "default-form", "de-DE"));

        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Equal("de", update.State.SelectedIsoCode);
        Assert.Equal("new-app", update.State.SelectedApplicationId);
        Assert.Equal("default-form", update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunVoiceCommandDialogEffect>(effect));
    }

    [Fact]
    public void Reduce_WhenApplicationSelectionCancelled_CompletesAndPreservesEffectiveIsoCode()
    {
        var state = CreateFormsCatalogState();

        var update = SpeechAssistantFormsCatalogReducer.Reduce(
            state,
            new SpeechAssistantApplicationSelectionCancelledEvent("en"));

        Assert.Equal(SpeechAssistantPhase.Completed, update.State.Phase);
        Assert.Null(update.State.ActiveDialog);
        Assert.Equal("en", update.State.SelectedIsoCode);
        Assert.Collection(update.Effects, effect => Assert.IsType<DispatchSpeechAssistantEventEffect>(effect));
    }

    private static SpeechAssistantState CreateFormsCatalogState()
        => SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            SelectedIsoCode = "de",
            CorrelationId = "corr"
        };
}
