using System.Collections.Immutable;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantSessionProjectionTests
{
    [Fact]
    public void ApplicationSessionStateProjector_WhenSubmitted_StoresValuesAndCompletedFormId()
    {
        var state = SpeechAssistantState.Initial;
        var result = VoiceCommandDialogResult.Submitted(
            " app ",
            " form ",
            ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = "Max"
                }));

        var sessions = SpeechAssistantApplicationSessionStateProjector.UpdateApplicationSessions(state, result);
        var updatedState = state with { ApplicationSessions = sessions };

        var completedFormIds = SpeechAssistantApplicationSessionStateProjector.GetCompletedFormIds(updatedState, "app");
        var initialValues = SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(updatedState, "app", "form");

        Assert.Contains("form", completedFormIds);
        Assert.Equal("Max", initialValues["name"]);
    }

    [Fact]
    public void InitialState_HasEmptySessions_SoHydrationIsEmpty()
    {
        var state = SpeechAssistantState.Initial;

        Assert.Empty(state.ApplicationSessions);
        var values = SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
            state,
            "app",
            "form");

        Assert.Empty(values);
    }

    [Fact]
    public void ReplacingFilledStateWithInitial_ClearsHydration()
    {
        var filled = SpeechAssistantState.Initial with
        {
            ApplicationSessions = SpeechAssistantApplicationSessionStateProjector.UpdateApplicationSessions(
                SpeechAssistantState.Initial,
                VoiceCommandDialogResult.Submitted(
                    "app",
                    "form",
                    ImmutableDictionary.CreateRange(
                        StringComparer.OrdinalIgnoreCase,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["name"] = "Max"
                        })))
        };

        Assert.Equal("Max", SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(filled, "app", "form")["name"]);

        var afterReset = SpeechAssistantState.Initial;
        var values = SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(afterReset, "app", "form");

        Assert.Empty(values);
    }

    [Fact]
    public void VoiceCommandCompletionReducer_WhenBackToAppSelection_ReturnsFormsCatalogState()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form"
        };

        var update = SpeechAssistantVoiceCommandCompletionReducer.Reduce(
            state,
            VoiceCommandDialogResult.BackToAppSelection(
                "app",
                "form",
                ImmutableDictionary<string, string>.Empty));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.FormsCatalog, update.State.ActiveDialog);
        Assert.Null(update.State.SelectedApplicationId);
        Assert.Null(update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunFormsCatalogDialogEffect>(effect));
    }

    [Fact]
    public void VoiceCommandCompletionReducer_WhenSwitchForm_ReturnsVoiceCommandState()
    {
        var state = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "old-form"
        };

        var update = SpeechAssistantVoiceCommandCompletionReducer.Reduce(
            state,
            VoiceCommandDialogResult.SwitchForm(
                "app",
                "old-form",
                "new-form",
                ImmutableDictionary<string, string>.Empty));

        Assert.Equal(SpeechAssistantPhase.Engaged, update.State.Phase);
        Assert.Equal(SpeechAssistantDialogKind.VoiceCommand, update.State.ActiveDialog);
        Assert.Equal("app", update.State.SelectedApplicationId);
        Assert.Equal("new-form", update.State.SelectedFormId);
        Assert.Collection(update.Effects, effect => Assert.IsType<RunVoiceCommandDialogEffect>(effect));
    }

    [Fact]
    public void VoiceCommandCompletionReducer_InstallsAlreadyCommittedTerminalValues()
    {
        var state = SpeechAssistantState.Initial with
        {
            SelectedIsoCode = "de",
            SelectedApplicationId = "app",
            SelectedFormId = "form",
        };
        var result = VoiceCommandDialogResult.Submitted(
            "app",
            "form",
            ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                new Dictionary<string, string> { ["name"] = "Max" }));

        var update = SpeechAssistantVoiceCommandCompletionReducer.Reduce(state, result);

        var installedValues = SpeechAssistantApplicationSessionStateProjector.GetInitialValuesByVariableId(
            update.State,
            "app",
            "form");
        Assert.Equal("Max", installedValues["name"]);
        Assert.DoesNotContain(update.Effects, effect => effect.GetType().Name.Contains("Evaluation", StringComparison.Ordinal));
    }
}
