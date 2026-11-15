using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantTraceCorrelationIdFlowTests
{
    [Fact]
    public async Task WakeWordActivatedFlow_PropagatesActivationCorrelationIdAcrossActivatedTurnTraceEntries()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance()));
        speechClient.EnqueueTranscript("Okay Alpha");

        var trace = new RecordingSpeechAssistantTrace();
        var voiceCommandDialogCalled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                It.IsAny<string>(),
                It.Is<SpeechTranscript>(transcript =>
                    transcript.Text == "alpha" && transcript.ConfidencePercent == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("test-app", "alpha", 1, "test-form"));

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
            NullLogger<SpeechAssistantService>.Instance,
            speechAssistantTrace: trace);

        await using var host = DialogHostFactory.Create<SpeechAssistantEvent>();
        await host.StartAsync(
            DialogFlowFactory.CreateReducerFlow(SpeechAssistantState.Initial, new SpeechAssistantReducer(), context),
            CancellationToken.None);

        await host.DispatchToActiveAsync(new SpeechAssistantLanguageSelectedEvent("de"), DispatchMode.Interrupt, CancellationToken.None);
        await voiceCommandDialogCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.NotEmpty(trace.Entries);
        Assert.Contains(trace.Entries, entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect);
        Assert.Contains(trace.Entries, entry => entry.Scope == SpeechAssistantTraceScope.FormsCatalogEffect);
        Assert.Contains(trace.Entries, entry => entry.Scope == SpeechAssistantTraceScope.VoiceCommandEffect);

        var activatedTurnCorrelationIds = trace.Entries
            .Where(entry =>
                entry.Scope == SpeechAssistantTraceScope.FormsCatalogEffect
                || entry.Scope == SpeechAssistantTraceScope.VoiceCommandEffect)
            .Select(entry => entry.CorrelationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var activatedTurnCorrelationId = Assert.Single(activatedTurnCorrelationIds);
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.CorrelationId == activatedTurnCorrelationId
                && entry.Message == "Wake word detected with inline command.");
    }

    private static CapturedSpeechAudio CreateUtterance()
        => new(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2, 3 }]);
}
