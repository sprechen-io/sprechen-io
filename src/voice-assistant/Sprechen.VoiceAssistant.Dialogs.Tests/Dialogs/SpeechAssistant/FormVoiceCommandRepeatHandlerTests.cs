using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandRepeatHandlerTests
{
    [Theory]
    [InlineData(GlobalCommandIds.Repeat, true)]
    [InlineData(GlobalCommandIds.Next, false)]
    [InlineData(GlobalCommandIds.Back, false)]
    [InlineData(GlobalCommandIds.Help, false)]
    [InlineData(GlobalCommandIds.Status, false)]
    public void CanHandle_ClaimsOnlyRepeat(string commandId, bool expected)
    {
        var handler = new FormVoiceCommandRepeatHandler(
            Mock.Of<IFormVoiceCommandSpokenOutputReplayBuffer>(),
            Mock.Of<IFormVoiceCommandSpeechService>(),
            Mock.Of<IFormVoiceCommandPromptService>());

        Assert.Equal(expected, handler.CanHandle(CommandId.From(commandId)));
    }

    [Fact]
    public async Task HandleAsync_ReplaysLastSpokenOutputVerbatim()
    {
        const string lastOutput = "Die Eingabe ist hier nicht gueltig.";
        var replayBuffer = new Mock<IFormVoiceCommandSpokenOutputReplayBuffer>(MockBehavior.Strict);
        replayBuffer.SetupGet(item => item.LastSpokenOutput).Returns(lastOutput);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(item => item.SpeakSafeAsync(
                lastOutput,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var handler = new FormVoiceCommandRepeatHandler(
            replayBuffer.Object,
            speechService.Object,
            promptService.Object);
        var context = CreateContext();

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        speechService.Verify(
            item => item.SpeakSafeAsync(
                lastOutput,
                "de",
                context.Selection.VoiceSettings,
                It.IsAny<CancellationToken>()),
            Times.Once);
        promptService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenNothingRecorded_ReReadsFocusedField()
    {
        var replayBuffer = new Mock<IFormVoiceCommandSpokenOutputReplayBuffer>(MockBehavior.Strict);
        replayBuffer.SetupGet(item => item.LastSpokenOutput).Returns((string?)null);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        promptService
            .Setup(item => item.SpeakHelpForFocusedFieldAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new FormVoiceCommandRepeatHandler(
            replayBuffer.Object,
            speechService.Object,
            promptService.Object);
        var context = CreateContext();

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        // Bei leerem Wiederholungsspeicher erhält der Hilfedienst den Zustandstracker der Sitzung.
        // So bleibt gespeichert, welche Befehlshinweise für dieses Feld bereits angefordert wurden.
        promptService.Verify(
            item => item.SpeakHelpForFocusedFieldAsync(
                context.Runtime.Interpreter,
                context.Runtime.StateTracker,
                "de",
                context.Selection.VoiceSettings,
                It.IsAny<CancellationToken>()),
            Times.Once);
        speechService.VerifyNoOtherCalls();
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext()
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary<string, string>.Empty);
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"],
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form],
        };

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.Repeat,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("repeat-session", EvaluationChannel.Voice, "app", "form"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
        };
    }
}
