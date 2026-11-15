using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandChangePolitenessHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenLanguageHasNoPolitenessProfile_SpeaksUnavailablePromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text => text.Contains("addressing mode", StringComparison.Ordinal)),
                "pt",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.PolitenessUnavailable,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "formal", isoCode: "pt"), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenArgumentIsMissing_SpeaksPromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text =>
                    text.Contains("sie", StringComparison.OrdinalIgnoreCase)
                    && text.Contains("du", StringComparison.OrdinalIgnoreCase)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.PolitenessMissingArgument,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: null), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenArgumentIsUnsupported_SpeaksPromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text =>
                    text.Contains("sie", StringComparison.OrdinalIgnoreCase)
                    && text.Contains("du", StringComparison.OrdinalIgnoreCase)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.PolitenessNotSupported,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "klingon"), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPolitenessIsAlreadySelected_SpeaksPromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text => text.Contains("sie", StringComparison.OrdinalIgnoreCase)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.PolitenessAlreadySelected,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "auf sie"), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPolitenessIsConfirmed_ReturnsPolitenessChangeResult()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.Is<string>(text => text.Contains("du", StringComparison.OrdinalIgnoreCase)),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "zu du"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VoiceCommandNavigationKind.ChangePoliteness, result.Navigation);
        Assert.Equal(PolitenessLevel.Informal, result.NavigationPolitenessLevel);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);
        speechService.VerifyAll();
        telemetry.VerifyNoOtherCalls();
    }

    private static FormVoiceCommandChangePolitenessHandler CreateHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        return new FormVoiceCommandChangePolitenessHandler(
            telemetry,
            new FormVoiceCommandPolitenessResolver(),
            speechService);
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(string? argument, string isoCode = "de")
    {
        var currentForm = new FormDescriptor
        {
            FormId = FormId.From("main-form"),
            DisplayName = new LocalizedText { ["de"] = "Hauptformular", ["en"] = "Main form" },
            Description = new LocalizedText { ["de"] = "Hauptformular", ["en"] = "Main form" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App", ["en"] = "App" },
            Description = new LocalizedText { ["de"] = "App", ["en"] = "App" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Forms = [currentForm]
        };

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = currentForm.FormId,
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main")
        };

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(runtime => runtime.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).Add("name", "Max"));

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.ChangePoliteness,
                IsoCode: isoCode,
                Argument: argument),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings
                {
                    PolitenessLevel = PolitenessLevel.Formal
                },
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: currentForm,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter.Object,
                Session: new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/main-form"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }
}
