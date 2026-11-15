using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

[Collection(SpeechAssistantEndToEndTestCollection.Name)]
public sealed class SpeechAssistantPauseResumeEndToEndTests
{
    private static readonly TimeSpan EndToEndTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan SimulatedUserResponseDelay = TimeSpan.FromMilliseconds(50);

    private const string ApplicationId = "test-app";
    private const string FormId = "main-form";
    private const string FieldVariableId = "firstName";
    private const string DictatedValue = "Max";

    [Fact]
    public async Task Workflow_WhenMicPausedMidVoiceCommand_PreservesDictatedValueAcrossResume()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var feedbackRecorders = fixture.CreateFeedbackRecorders();
        var textToSpeechPlayer = feedbackRecorders.TextToSpeechPlayer;
        var earconPlayer = feedbackRecorders.EarconPlayer;
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Die Zustimmung zur Sprache wird vor der Formulareingabe eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 2);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript(DictatedValue);

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"]);
        var voiceCommandDialog = new PauseThenSubmitVoiceCommandDialog(speechInputClient, FieldVariableId);

        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            wakeWordProvider,
            new FixedFormsCatalogDialog(ApplicationId, "Test Application", 1, FormId),
            new FixedFormSelectionDialog(FormId),
            voiceCommandDialog,
            NullLogger<SpeechAssistantService>.Instance);

        await using var assistant = new SpeechAssistantService(assistantContext);

        var feedbackCursor = fixture.GetFeedbackCursor(feedbackRecorders.ReadFeedback);
        await assistant.StartAsync(CancellationToken.None);

        // Warten, bis der erste Feldwert erfasst ist und der Testdialog auf die nächste Eingabe wartet.
        await fixture.WaitUntilAsync(() => voiceCommandDialog.FirstFieldDictated, EndToEndTimeout);
        await fixture.AssertFeedbackProducedAfterAsync(
            feedbackRecorders.ReadFeedback,
            feedbackCursor,
            EndToEndTimeout,
            "pause-resume dialog start");

        // Die nächste Unterbrechung erst nach dem ersten Feldwert als Mikrofonpause behandeln, damit frühere Abbrüche den Test nicht auslösen.
        speechInputClient.ArmVoiceInputPauseOnNextCancellation();
        var pausedActivationState = await assistant.ToggleVoiceInputAsync(CancellationToken.None);
        Assert.Equal(SpeechAssistantActivationState.Paused, pausedActivationState);

        await fixture.WaitUntilAsync(
            () => TryReadDictatedValue(ReadAssistantState(assistant)) == DictatedValue,
            EndToEndTimeout);

        var pausedState = ReadAssistantState(assistant);
        Assert.Equal(SpeechAssistantPhase.Paused, pausedState.Phase);
        Assert.Equal(DictatedValue, TryReadDictatedValue(pausedState));


        var resumedActivationState = await assistant.ToggleVoiceInputAsync(CancellationToken.None);
        Assert.Equal(SpeechAssistantActivationState.Ready, resumedActivationState);

        await fixture.WaitUntilAsync(() => voiceCommandDialog.Executions.Count >= 2, EndToEndTimeout);

        var resumedExecution = voiceCommandDialog.Executions[1];
        Assert.Equal(ApplicationId, resumedExecution.ApplicationId);
        Assert.Equal(FormId, resumedExecution.FormId);
        Assert.True(
            resumedExecution.InitialValuesByVariableId.TryGetValue(FieldVariableId, out var resumedValue),
            "Resumed voice-command dialog did not receive the value dictated before the pause.");
        Assert.Equal(DictatedValue, resumedValue);

        await fixture.WaitUntilAsync(
            () => IsSubmittedWithDictatedValue(ReadAssistantState(assistant)),
            EndToEndTimeout);
    }

    [Fact]
    public async Task Workflow_WhenVoiceCommandDialogFailsAfterValueExists_AnnouncesFailureAndPreservesValue()
    {
        var fixture = new SpeechAssistantEndToEndFixture();
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        var interactionSignals = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var speechInputClient = new QueuedSpeechInputClient(SimulatedUserResponseDelay);
        var feedbackRecorders = fixture.CreateFeedbackRecorders();
        var textToSpeechPlayer = feedbackRecorders.TextToSpeechPlayer;
        var earconPlayer = feedbackRecorders.EarconPlayer;
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        // Die Zustimmung zur Sprache wird vor den Eingaben für den Fehlerfall eingereiht.
        await fixture.EnqueueKeptSessionEntryLanguageAsync(speechInputClient, userLanguageState);
        await fixture.EnqueueUtterancesAsync(speechInputClient, 4);
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");
        speechInputClient.EnqueueTranscript("Okay bitte Anwendungskatalog öffnen");

        var expectedFailureText = ResolveResourceText(
            VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed,
            "de");
        _ = interactionSignals.SubscribeToFailures(message =>
        {
            var resourceId = message.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed
                ? VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed
                : VoiceInterfaceResourceIds.SpeechCaptureFailureDefault;
            var text = ResolveResourceText(resourceId, userLanguageState.SelectedIsoCode ?? "de");
            return new ValueTask(textToSpeechPlayer.SpeakAsync(text, userLanguageState.SelectedIsoCode ?? "de", CancellationToken.None));
        });

        var voiceCommandDialog = new AbandonWithValueThenFailVoiceCommandDialog(FieldVariableId, DictatedValue);
        var assistantContext = SpeechAssistantContextTestFactory.Create(
            interactionSignals,
            userLanguageState,
            speechInputClient,
            textToSpeechPlayer,
            earconPlayer,
            new FixedWakeWordProvider(["Okay"]),
            new FixedFormsCatalogDialog(ApplicationId, "Test Application", 1, FormId),
            new FixedFormSelectionDialog(FormId),
            voiceCommandDialog,
            NullLogger<SpeechAssistantService>.Instance);

        await using var assistant = new SpeechAssistantService(assistantContext);

        var feedbackCursor = fixture.GetFeedbackCursor(feedbackRecorders.ReadFeedback);

        await assistant.StartAsync(CancellationToken.None);

        await fixture.WaitUntilAsync(() => voiceCommandDialog.ExecutionCount >= 2, EndToEndTimeout);
        await fixture.WaitUntilAsync(
            () => textToSpeechPlayer.Calls.Any(call => call.Text == expectedFailureText && call.LanguageCode == "de"),
            EndToEndTimeout);
        await fixture.WaitUntilAsync(
            () => ReadAssistantState(assistant).Phase == SpeechAssistantPhase.Armed
                && TryReadDictatedValue(ReadAssistantState(assistant)) == DictatedValue,
            EndToEndTimeout);
        await fixture.AssertFeedbackProducedAfterAsync(
            feedbackRecorders.ReadFeedback,
            feedbackCursor,
            EndToEndTimeout,
            "voice-command dialog failure recovery");

        Assert.Contains(
            interactionSignals.Failures,
            failure => failure.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed);
        Assert.Contains(
            voiceCommandDialog.InitialValuesByExecution.Skip(1),
            values => values.TryGetValue(FieldVariableId, out var value) && value == DictatedValue);
        Assert.Contains(EarconId.FlowListening, earconPlayer.PlayedEarcons);
    }

    private static bool IsSubmittedWithDictatedValue(SpeechAssistantState state)
    {
        if (!state.ApplicationSessions.TryGetValue(ApplicationId, out var application))
        {
            return false;
        }

        return application.FormsById.TryGetValue(FormId, out var form)
            && form.IsSubmitted
            && form.ValuesByVariableId.TryGetValue(FieldVariableId, out var value)
            && string.Equals(value, DictatedValue, StringComparison.Ordinal);
    }

    private static string? TryReadDictatedValue(SpeechAssistantState state)
    {
        if (!state.ApplicationSessions.TryGetValue(ApplicationId, out var application))
        {
            return null;
        }

        if (!application.FormsById.TryGetValue(FormId, out var form))
        {
            return null;
        }

        return form.ValuesByVariableId.TryGetValue(FieldVariableId, out var value) ? value : null;
    }

    private static SpeechAssistantState ReadAssistantState(SpeechAssistantService assistant)
    {
        var lifecycle = GetRequiredField(assistant, "_lifecycle");
        var host = GetRequiredField(lifecycle, "_host");
        var session = GetRequiredField(host, "_session");
        var stack = GetRequiredField(session, "_stack");
        if (GetRequiredField(stack, "_modules") is not IList { Count: > 0 } modules)
        {
            throw new InvalidOperationException("Dialog host stack is empty.");
        }

        var machine = GetRequiredField(modules[^1]!, "_machine");
        var stateProperty = machine.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("State machine does not expose a State property.");

        return (SpeechAssistantState)stateProperty.GetValue(machine)!;
    }

    private static object GetRequiredField(object target, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(target);

        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on '{target.GetType().Name}'.");

        return field.GetValue(target)
            ?? throw new InvalidOperationException($"Field '{fieldName}' on '{target.GetType().Name}' was null.");
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
