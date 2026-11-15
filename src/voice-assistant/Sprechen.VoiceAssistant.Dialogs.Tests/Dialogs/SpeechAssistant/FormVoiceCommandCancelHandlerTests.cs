using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandCancelHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCancelRequestedTwiceWithinRefractory_SpeaksRefractoryPrompt()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 2, 8, 15, 0, 0, TimeSpan.Zero));
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                FormConfirmationPromptBuilder.GetCancelRefractoryPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: false));
        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: true));

        var handler = new FormVoiceCommandCancelHandler(clock, telemetry.Object, speechService.Object);
        var context = CreateContext(sessionId: "session-1", out var transitions);

        var firstResult = await handler.HandleAsync(context, CancellationToken.None);
        var secondResult = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(firstResult);
        Assert.Null(secondResult);
        Assert.Equal([DialogStates.CancelConfirmation, DialogStates.ErrorPrompt], transitions);
        speechService.Verify(service => service.ConfirmYesNoAsync(
            It.IsAny<FormVoiceCommandCaptureSettings>(),
            It.IsAny<string>(),
            "de",
            It.IsAny<SpeechCaptureTelemetryContext?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        speechService.Verify(service => service.SpeakSafeAsync(
            FormConfirmationPromptBuilder.GetCancelRefractoryPrompt("de"),
            "de",
            It.IsAny<VoiceDialogSettings>(),
            It.IsAny<CancellationToken>()), Times.Once);
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRefractoryElapsed_AllowsCancelConfirmationAgain()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 2, 8, 15, 0, 0, TimeSpan.Zero));
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .SetupSequence(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: false));
        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: true,
                refractoryBlocked: false));

        var handler = new FormVoiceCommandCancelHandler(clock, telemetry.Object, speechService.Object);
        var context = CreateContext(sessionId: "session-1", out _);

        var firstResult = await handler.HandleAsync(context, CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddSeconds(3);
        var secondResult = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(firstResult);
        Assert.NotNull(secondResult);
        Assert.True(secondResult.WasCancelled);
        speechService.Verify(service => service.ConfirmYesNoAsync(
            It.IsAny<FormVoiceCommandCaptureSettings>(),
            It.IsAny<string>(),
            "de",
            It.IsAny<SpeechCaptureTelemetryContext?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        speechService.Verify(service => service.SpeakSafeAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<VoiceDialogSettings>(),
            It.IsAny<CancellationToken>()), Times.Never);
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenConfiguredRefractoryNotElapsed_BlocksCancelConfirmationAgain()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 2, 8, 15, 0, 0, TimeSpan.Zero));
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                FormConfirmationPromptBuilder.GetCancelRefractoryPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: false));
        telemetry
            .Setup(t => t.TrackDestructiveCommandResolved(
                It.IsAny<EvaluationSession>(),
                GlobalCommandIds.Cancel,
                confirmed: false,
                refractoryBlocked: true));

        var handler = new FormVoiceCommandCancelHandler(clock, telemetry.Object, speechService.Object);
        var context = CreateContext(
            sessionId: "session-1",
            out _,
            new VoiceDialogSettings { CancelRefractoryMs = 5000 });

        var firstResult = await handler.HandleAsync(context, CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddSeconds(3);
        var secondResult = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(firstResult);
        Assert.Null(secondResult);
        speechService.Verify(service => service.ConfirmYesNoAsync(
            It.IsAny<FormVoiceCommandCaptureSettings>(),
            It.IsAny<string>(),
            "de",
            It.IsAny<SpeechCaptureTelemetryContext?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        speechService.Verify(service => service.SpeakSafeAsync(
            FormConfirmationPromptBuilder.GetCancelRefractoryPrompt("de"),
            "de",
            It.IsAny<VoiceDialogSettings>(),
            It.IsAny<CancellationToken>()), Times.Once);
        telemetry.VerifyAll();
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(
        string sessionId,
        out List<string> transitions,
        VoiceDialogSettings? voiceSettings = null)
    {
        var contract = CreateContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: "de", ImmutableDictionary<string, string>.Empty);

        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        transitions = [];

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.Cancel,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings ?? new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession(sessionId, EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker(transitions)));
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
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptteil" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }

}
