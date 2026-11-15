using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantWorkflowTests
{
    [Fact]
    public async Task Workflow_WhenWakeWordAndCommandShareUtterance_InvokesFormsCatalogDialogWithSeedTranscript()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Okay Alpha");

        var catalogDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                It.IsAny<string>(),
                It.Is<AppSpeechTranscript>(transcript =>
                    transcript.Text == "alpha" && transcript.ConfidencePercent == null),
                It.IsAny<CancellationToken>()))
            .Callback(() => catalogDialogCalled.TrySetResult(true))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("test-app", "alpha", 1, "test-form"));

        var voiceCommandDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                "test-form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => voiceCommandDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog.Object,
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(SpeechAssistantState.Initial, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(new SpeechAssistantLanguageSelectedEvent("de"), DispatchMode.Interrupt, CancellationToken.None);

        await catalogDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await voiceCommandDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        catalogDialog.VerifyAll();
        voiceCommandDialog.VerifyAll();
        Assert.True(speechClient.RecordCallCount >= 1);
        Assert.True(speechClient.SpeechToTextCallCount >= 1);

        Assert.Equal(SpeechCaptureMode.ActivationListening, speechClient.RecordModes[0]);
    }

    [Fact]
    public async Task Workflow_WhenWakeWordDetectedWithoutCommand_OpensFormsCatalogAnnouncementWithoutGenericInvitation()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        await speechClient.EnqueueRecordStepAsync(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return CreateUtterance();
        });
        speechClient.EnqueueTranscript("Okay");
        var textToSpeechPlayer = new RecordingTextToSpeechPlayer();

        // Ein reines Aktivierungswort öffnet den Katalog ohne vorweggenommenes Transkript.
        // Der Dialog übernimmt Ankündigung und weitere Aufnahme; die Steuerung ergänzt keine allgemeine Einladung.
        var catalogDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync("de", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => catalogDialogCalled.TrySetResult(true))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("test-app", "Beispiel", 1, "test-form"));

        var voiceCommandDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                "test-form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => voiceCommandDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            textToSpeechPlayer,
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog.Object,
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(SpeechAssistantState.Initial, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(new SpeechAssistantLanguageSelectedEvent("de"), DispatchMode.Interrupt, CancellationToken.None);

        await catalogDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await voiceCommandDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        catalogDialog.VerifyAll();
        voiceCommandDialog.VerifyAll();
        Assert.True(speechClient.RecordCallCount >= 1);
        Assert.True(speechClient.SpeechToTextCallCount >= 1);
        Assert.Equal(SpeechCaptureMode.ActivationListening, speechClient.RecordModes[0]);
        Assert.DoesNotContain(
            textToSpeechPlayer.Calls,
            call => call.Text == "Wie kann ich helfen?");
    }

    [Fact]
    public async Task Workflow_WhenServiceStarted_RunsWakeWordVoiceCommand()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()))
            .Returns((string? isoCode, CancellationToken _) =>
            {
                userLanguageState.SetSelectedIsoCode(isoCode!);
                return ValueTask.FromResult(new UserLanguageSettings(isoCode!, userLanguageState.SelectedPolitenessLevel));
            });

        var speechClient = new ScriptedSpeechInputClient();
        // Der echte Sitzungsstart verbraucht zuerst eine Antwort auf die Sprachfrage.
        // Deutsch nimmt diese erste Frage an; Nein würde weitere Fragen verlangen, für die hier keine Antworten vorbereitet sind.
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Deutsch");

        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Okay Alpha");

        var catalogDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                It.IsAny<string>(),
                It.Is<AppSpeechTranscript>(transcript =>
                    transcript.Text == "alpha" && transcript.ConfidencePercent == null),
                It.IsAny<CancellationToken>()))
            .Callback(() => catalogDialogCalled.TrySetResult(true))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("test-app", "alpha", 1, "test-form"));

        var voiceCommandDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                "test-form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => voiceCommandDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            userLanguageState,
            workflow.Object,
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog.Object,
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context);

        // Das Warten auf den Sitzungsstart ist auf zehn Sekunden begrenzt.
        await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        await catalogDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await voiceCommandDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        workflow.VerifyAll();
        catalogDialog.VerifyAll();
        voiceCommandDialog.VerifyAll();
        Assert.True(speechClient.RecordCallCount >= 1);
        Assert.True(speechClient.SpeechToTextCallCount >= 1);
        // Die Folge wird vollständig geprüft: zuerst ShortConfirmation für die Sprachwahl, danach ActivationListening. Ein Filter würde Änderungen am Einstieg verbergen.
        Assert.Equal(SpeechCaptureMode.ShortConfirmation, speechClient.RecordModes[0]);
        Assert.Equal(SpeechCaptureMode.ActivationListening, speechClient.RecordModes[1]);
    }

    [Fact]
    public async Task Workflow_WhenStartedAndToggled_PausesAndResumesWakeWordListener()
    {
        var firstRecordStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRecordCancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRecordStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var speechClient = new ScriptedSpeechInputClient();
        // Vor der Aktivierung beantwortet Deutsch die erste Sprachfrage. Die weiteren
        // vorbereiteten Aufnahmen gehören dadurch zum Aktivierungsablauf.
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Deutsch");

        await speechClient.EnqueueRecordStepAsync(async cancellationToken =>
        {
            firstRecordStarted.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                firstRecordCancelled.TrySetResult(true);
                throw;
            }

            return CreateUtterance();
        });
        await speechClient.EnqueueRecordStepAsync(async cancellationToken =>
        {
            secondRecordStarted.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return CreateUtterance();
        });

        // Deutsch ausdrücklich setzen, damit die vorbereitete Antwort zur ersten Frage passt.
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            userLanguageState,
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context);

        // Das Warten auf den Sitzungsstart ist auf zehn Sekunden begrenzt.
        await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await firstRecordStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var pausedState = await service.ToggleVoiceInputAsync(CancellationToken.None);

        Assert.Equal(SpeechAssistantActivationState.Paused, pausedState);
        await firstRecordCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var resumedState = await service.ToggleVoiceInputAsync(CancellationToken.None);

        Assert.Equal(SpeechAssistantActivationState.Ready, resumedState);
        await secondRecordStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Das Antwortfenster des Einstiegs muss vor den beiden Aktivierungsaufnahmen liegen.
        Assert.Equal(SpeechCaptureMode.ShortConfirmation, speechClient.RecordModes[0]);
        Assert.Equal(
            [SpeechCaptureMode.ActivationListening, SpeechCaptureMode.ActivationListening],
            speechClient.RecordModes.Skip(1).Take(2).ToArray());
    }

    [Fact]
    public async Task Workflow_WhenArmedAndNoWakeWord_DoesNotInvokeVoiceCommandDialog()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(CreateUtterance());
        await speechClient.EnqueueRecordingAsync(CreateUtterance());

        speechClient.EnqueueTranscript("ja");
        speechClient.EnqueueTranscript("nein");

        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        var initialState = SpeechAssistantState.Initial with { Phase = SpeechAssistantPhase.Completed, SelectedIsoCode = "de" };

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(DialogFlowFactory.CreateReducerFlow(initialState, new SpeechAssistantReducer(), context), CancellationToken.None);

        await host.DispatchToActiveAsync(new SpeechAssistantReturnToArmedEvent(), DispatchMode.Interrupt, CancellationToken.None);

        await speechClient.WaitForRecordCallCountAsync(2, TimeSpan.FromSeconds(2));

        voiceCommandDialog.Verify(
            dialog => dialog.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Workflow_WhenVoiceCommandChangesLanguage_RestartsDialogWithNewLanguageAndPreservedValues()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.SaveSelectedLanguageAsync("en", It.IsAny<CancellationToken>()))
            .Returns((string? isoCode, CancellationToken _) =>
            {
                userLanguageState.SetSelectedIsoCode(isoCode!);
                return ValueTask.FromResult(new UserLanguageSettings(isoCode!, userLanguageState.SelectedPolitenessLevel));
            });

        var resumedDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "en",
                "test-app",
                "test-form",
                It.Is<SpeechAssistantApplicationSessionState>(session => HasFormValue(session, "test-form", "name", "Max")),
                It.Is<IReadOnlyDictionary<string, string>>(values => HasValue(values, "name", "Max")),
                It.IsAny<CancellationToken>()))
            .Callback(() => resumedDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("name", "Max")));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            userLanguageState,
            workflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        var initialState = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "test-app",
            SelectedFormId = "test-form"
        };

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(initialState, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(
            new SpeechAssistantVoiceCommandCompletedEvent(
                VoiceCommandDialogResult.ChangeLanguage(
                    "test-app",
                    "test-form",
                    "en",
                    ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("name", "Max"))),
            DispatchMode.Enqueue,
            CancellationToken.None);

        await resumedDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("en", userLanguageState.SelectedIsoCode);
        workflow.VerifyAll();
        voiceCommandDialog.VerifyAll();
    }

    [Fact]
    public async Task Workflow_WhenCatalogChangesLanguage_ContinuesVoiceCommandWithTheEffectiveIsoCode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Okay Alpha");

        var catalogDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                It.IsAny<string>(),
                It.Is<AppSpeechTranscript>(transcript =>
                    transcript.Text == "alpha" && transcript.ConfidencePercent == null),
                It.IsAny<CancellationToken>()))
            .Callback(() => catalogDialogCalled.TrySetResult(true))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("test-app", "alpha", 1, "test-form", "en"));

        var voiceCommandDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "en",
                "test-app",
                "test-form",
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => voiceCommandDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog.Object,
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(SpeechAssistantState.Initial, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(new SpeechAssistantLanguageSelectedEvent("de"), DispatchMode.Interrupt, CancellationToken.None);

        await catalogDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await voiceCommandDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        catalogDialog.VerifyAll();
        voiceCommandDialog.VerifyAll();
    }

    [Fact]
    public async Task Workflow_WhenVoiceCommandChangesPoliteness_RestartsDialogWithSameLanguageAndPreservedValues()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.SaveSelectedPolitenessAsync(PolitenessLevel.Informal, It.IsAny<CancellationToken>()))
            .Returns((PolitenessLevel? politenessLevel, CancellationToken _) =>
            {
                userLanguageState.SetSelectedPolitenessLevel(politenessLevel);
                return ValueTask.FromResult(new UserLanguageSettings(userLanguageState.SelectedIsoCode, politenessLevel));
            });

        var resumedDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                "test-form",
                It.Is<SpeechAssistantApplicationSessionState>(session => HasFormValue(session, "test-form", "name", "Max")),
                It.Is<IReadOnlyDictionary<string, string>>(values => HasValue(values, "name", "Max")),
                It.IsAny<CancellationToken>()))
            .Callback(() => resumedDialogCalled.TrySetResult(true))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("name", "Max")));

        var context = SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            userLanguageState,
            workflow.Object,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            voiceCommandDialog.Object,
            NullLogger<SpeechAssistantService>.Instance);

        var initialState = SpeechAssistantState.Initial with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            SelectedIsoCode = "de",
            SelectedApplicationId = "test-app",
            SelectedFormId = "test-form"
        };

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(initialState, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(
            new SpeechAssistantVoiceCommandCompletedEvent(
                VoiceCommandDialogResult.ChangePoliteness(
                    "test-app",
                    "test-form",
                    PolitenessLevel.Informal,
                    ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("name", "Max"))),
            DispatchMode.Enqueue,
            CancellationToken.None);

        await resumedDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("de", userLanguageState.SelectedIsoCode);
        Assert.Equal(PolitenessLevel.Informal, userLanguageState.SelectedPolitenessLevel);
        workflow.VerifyAll();
        voiceCommandDialog.VerifyAll();
    }

    private static CapturedSpeechAudio CreateUtterance()
    {
        return new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2, 3 } });
    }

    private static bool HasFormValue(
        SpeechAssistantApplicationSessionState session,
        string formId,
        string variableId,
        string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);
        ArgumentNullException.ThrowIfNull(expectedValue);

        return session.FormsById.TryGetValue(formId, out var formSession)
            && HasValue(formSession.ValuesByVariableId, variableId, expectedValue);
    }

    private static bool HasValue(
        IReadOnlyDictionary<string, string> values,
        string variableId,
        string expectedValue)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);
        ArgumentNullException.ThrowIfNull(expectedValue);

        return values.TryGetValue(variableId, out var actualValue)
            && string.Equals(actualValue, expectedValue, StringComparison.Ordinal);
    }
}
