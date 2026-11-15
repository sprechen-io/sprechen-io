using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandChangeLanguageHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenArgumentIsMissing_SpeaksPromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text =>
                    text.Contains("Deutsch", StringComparison.Ordinal)
                    && text.Contains("Englisch", StringComparison.Ordinal)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.LanguageMissingArgument,
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
                    text.Contains("Deutsch", StringComparison.Ordinal)
                    && text.Contains("Englisch", StringComparison.Ordinal)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.LanguageNotSupported,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "klingon"), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsAlreadySelected_SpeaksPromptAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text => text.Contains("Deutsch", StringComparison.Ordinal)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.LanguageAlreadySelected,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "auf deutsch"), CancellationToken.None);

        Assert.Null(result);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageIsConfirmed_ReturnsLanguageChangeResult()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.Is<string>(text => text.Contains("Englisch", StringComparison.Ordinal)),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.IsAny<string>(),
                "en",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "to english"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VoiceCommandNavigationKind.ChangeLanguage, result.Navigation);
        Assert.Equal("en", result.NavigationLanguageIsoCode);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);
        speechService.VerifyAll();
        telemetry.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenConfirmed_SpeaksTargetLanguageWakeWordHint()
    {
        var expectedHint = FormDialogFlowPromptTextResolver.GetWakeWordHintPrompt("en");

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.Is<string>(text => text.Contains("Englisch", StringComparison.Ordinal)),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                It.Is<string>(text => string.Equals(text, expectedHint, StringComparison.Ordinal)),
                "en",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "to english"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("en", result.NavigationLanguageIsoCode);
        speechService.Verify(
            service => service.SpeakSafeAsync(
                It.Is<string>(text => string.Equals(text, expectedHint, StringComparison.Ordinal)),
                "en",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenDeclined_DoesNotSpeakWakeWordHintAndReturnsNull()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.Is<string>(text => text.Contains("Englisch", StringComparison.Ordinal)),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var handler = CreateHandler(telemetry.Object, speechService.Object);

        var result = await handler.HandleAsync(CreateContext(argument: "to english"), CancellationToken.None);

        Assert.Null(result);
        speechService.Verify(
            service => service.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        telemetry.VerifyNoOtherCalls();
    }

    private static FormVoiceCommandChangeLanguageHandler CreateHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        return new FormVoiceCommandChangeLanguageHandler(
            telemetry,
            new LanguageChangeTargetResolver(CreateLanguageCatalog()),
            speechService);
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(string? argument)
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
                    Label = new LocalizedText { ["de"] = "Name", ["en"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main", ["en"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };

        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            new Dictionary<string, string>
            {
                ["name"] = "Max"
            });

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.ChangeLanguage,
                IsoCode: "de",
                Argument: argument),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: currentForm,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/main-form"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static ILanguageCatalog CreateLanguageCatalog()
    {
        return new VoiceInterfaceLanguageCatalog(
            new SupportedLanguages(
                VoiceInterfaceModelCatalog.DefaultLanguage,
                VoiceInterfaceModelCatalog.SupportedLanguages
                    .Select(isoCode => new SupportedLanguage(
                        isoCode,
                        ResolveLanguageEndonym(isoCode)))
                    .ToArray()));
    }

    private static string ResolveLanguageEndonym(string isoCode)
    {
        if (VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
                $"voice.languageName.{isoCode}",
                isoCode,
                out var endonym))
        {
            return endonym;
        }

        return isoCode;
    }
}
