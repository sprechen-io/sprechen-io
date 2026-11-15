using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandInputModeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSpellingCommand_SetsSpellingModeAndSpeaksFeedback()
    {
        var telemetry = CreateTelemetry(InputModes.Voice, InputModes.Spelling);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync("Buchstabiermodus aktiv. Nutzen Sie am besten die Buchstabiertafel, zum Beispiel Anton, Berta, Dora. Sagen Sie zurück, um das letzte Zeichen zu löschen, wiederholen, um das Erfasste zu hören, oder fertig, wenn der Wert vollständig ist.", "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeSpelling);

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Spelling, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenVoiceCommand_SetsVoiceModeAndSpeaksFeedback()
    {
        var telemetry = CreateTelemetry(InputModes.Spelling, InputModes.Voice);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync("Normale Spracheingabe aktiv.", "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeVoice);
        Assert.True(await context.Interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Spelling));

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenKeyboardCommand_ReturnsSilentInputRequestedWithoutOverride()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeKeyboard);

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsSilentInputRequested);
        Assert.True(result.WasPaused);
        Assert.Equal("name", result.SilentInputEligibility!.FieldId);
        Assert.Equal(SpeechAssistantSilentInputControlKind.Text, result.SilentInputEligibility.ControlKind);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyNoOtherCalls();
        telemetry.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenKeyboardCommandTargetsUnsupportedField_SpeaksUnavailableAndContinues()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                "Stille Eingabe ist für dieses Feld nicht verfügbar.",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeKeyboard, """{"kind":"boolean"}""");

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenDigitsCommand_SetsDigitsModeAndSpeaksFeedback()
    {
        // Der Formathinweis phone macht den Ziffernmodus für dieses Textfeld verfügbar.
        var telemetry = CreateTelemetry(InputModes.Voice, InputModes.Digits);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync("Ziffernmodus aktiv.", "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeDigits, """{"kind":"string","formatHint":"phone"}""");

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Digits, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLiteralCommand_SetsLiteralModeAndSpeaksFeedback()
    {
        var telemetry = CreateTelemetry(InputModes.Voice, InputModes.Literal);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync("W\u00f6rtlichmodus aktiv.", "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeLiteral);

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Literal, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLiteralOffCommand_SetsVoiceModeAndSpeaksFeedback()
    {
        var telemetry = CreateTelemetry(InputModes.Literal, InputModes.Voice);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync("Normale Spracheingabe aktiv.", "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeLiteralOff);
        Assert.True(await context.Interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Literal));

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSpellingCommandOnSensitiveField_SetsSpellingModeWithoutAQuestion()
    {
        var telemetry = CreateTelemetry(InputModes.Voice, InputModes.Spelling);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.SpeakSafeAsync(
                "Buchstabiermodus aktiv. Nutzen Sie am besten die Buchstabiertafel, zum Beispiel Anton, Berta, Dora. Sagen Sie zurück, um das letzte Zeichen zu löschen, wiederholen, um das Erfasste zu hören, oder fertig, wenn der Wert vollständig ist.",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(
            GlobalCommandIds.InputModeSpelling,
            """{"kind":"string"}""",
            privacyClass: PrivacyClass.Sensitive,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
            });

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Spelling, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    // Ein für das Wahrheitswertfeld ungeeigneter Modus darf weder gesetzt noch als Moduswechsel erfasst werden.
    [Fact]
    public async Task HandleAsync_WhenSpellingCommandTargetsBooleanField_DoesNotChangeModeOrTrackIt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeSpelling, """{"kind":"boolean"}""");

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        telemetry.Verify(
            t => t.TrackInputModeChanged(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDigitsCommandTargetsBooleanField_DoesNotChangeModeOrTrackIt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        var handler = new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(GlobalCommandIds.InputModeDigits, """{"kind":"boolean"}""");

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(InputMode.Voice, (await context.Interpreter.GetFocusedFieldAsync()).InputMode);
        telemetry.Verify(
            t => t.TrackInputModeChanged(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static Mock<IEvaluationTelemetry> CreateTelemetry(string fromMode, string toMode)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackInputModeChanged(
                It.IsAny<EvaluationSession>(),
                "name",
                fromMode,
                toMode,
                TelemetryInputModeTriggers.ExplicitCommand));
        return telemetry;
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(
        string commandId,
        string valueTypeJson = """{"kind":"string"}""",
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null)
    {
        var contract = CreateContract(valueTypeJson, privacyClass, sensitiveDataPolicy);
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

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: commandId,
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
                Session: new EvaluationSession("session", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static FormContract CreateContract(
        string valueTypeJson = """{"kind":"string"}""",
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null)
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
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = sensitiveDataPolicy,
                    ValueType = JsonDocument.Parse(valueTypeJson).RootElement
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
