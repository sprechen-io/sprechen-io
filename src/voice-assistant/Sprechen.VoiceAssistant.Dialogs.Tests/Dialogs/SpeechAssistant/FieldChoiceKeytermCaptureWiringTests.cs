using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Übergabe vorgegebener Optionsnamen als Erkennungshinweise an den Sprachdienst-Mock.
/// Ein Freitextfeld ohne Optionen liefert keine solchen Hinweise. Außerdem werden die
/// Aufnahmezeitvorgaben für Auswahlfelder und einfache Felder geprüft.
/// Lokalisierung, Erkennerschema und Weitergabe bis zum Client werden hier nicht vollständig durchlaufen.
/// </summary>
public sealed class FieldChoiceKeytermCaptureWiringTests
{
    [Theory]
    [InlineData("Blau", "Rot", "Grün")]     // de
    [InlineData("Blue", "Red", "Green")]    // en
    [InlineData("Bleu", "Rouge", "Vert")]   // fr
    [InlineData("Azul", "Rojo", "Verde")]   // es
    [InlineData("Blu", "Rosso", "Verde")]   // it
    public async Task CaptureAsync_EnumField_FeedsOptionDisplayNamesAsKeyterms(
        string first, string second, string third)
    {
        SpeechKeyterms? capturedKeyterms = null;
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, keyterms, _) =>
                {
                    capturedKeyterms = keyterms;
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult(first, null));
                });

        var options = ImmutableArray.Create(
            new FormChoiceOption("c1", first),
            new FormChoiceOption("c2", second),
            new FormChoiceOption("c3", third));
        var (capture, turnContext) = CreateCapture(speechService.Object, ValueTypeKinds.Enum, options);

        var result = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        // Die Optionsnamen werden unverändert an den Erfassungsaufruf übergeben.
        Assert.Equal(first, result.RequiredTranscript);
        Assert.NotNull(capturedKeyterms);
        Assert.Equal(new[] { first, second, third }, capturedKeyterms!.Terms);
        // Die feldbezogenen Hinweise ergänzen das serverseitige Grundvokabular.
        Assert.Equal(KeytermComposition.WithServerBase, capturedKeyterms.Composition);
    }

    [Fact]
    public async Task CaptureAsync_FreeTextField_FeedsNoKeyterms()
    {
        // Das Freitextfeld hat keine Optionen und darf deshalb keine feldbezogenen Erkennungshinweise liefern.
        SpeechKeyterms? capturedKeyterms = null;
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, keyterms, _) =>
                {
                    capturedKeyterms = keyterms;
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("max mustermann", null));
                });

        var (capture, turnContext) = CreateCapture(speechService.Object, ValueTypeKinds.String, options: []);

        var result = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.Equal("max mustermann", result.RequiredTranscript);
        Assert.Null(capturedKeyterms);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ValueTypeKinds.Enum)]
    [InlineData(ValueTypeKinds.Set)]
    public async Task CaptureAsync_OptionListingField_UsesModeledInitialSilenceTimeout(string valueTypeKind)
    {
        FormVoiceCommandCaptureSettings? receivedCaptureSettings = null;
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, captureSettings, onStartListening, _, _, _, _) =>
                {
                    receivedCaptureSettings = captureSettings;
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Blau", 95));
                });

        var voiceSettings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 0,
            SttMinConfidenceToAccept = 91,
            SttMinConfidenceToConfirm = 73
        };
        var options = ImmutableArray.Create(
            new FormChoiceOption("c1", "Blau"),
            new FormChoiceOption("c2", "Rot"));
        var (capture, turnContext) = CreateCapture(
            speechService.Object,
            valueTypeKind,
            options,
            voiceSettings);

        await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.NotNull(receivedCaptureSettings);
        Assert.Equal(18000, receivedCaptureSettings.VoiceSettings.NoInputTimeoutMs);
        Assert.Equal(91, receivedCaptureSettings.VoiceSettings.SttMinConfidenceToAccept);
        Assert.Equal(73, receivedCaptureSettings.VoiceSettings.SttMinConfidenceToConfirm);
    }

    [Theory]
    [InlineData(ValueTypeKinds.Integer)]
    [InlineData(ValueTypeKinds.Boolean)]
    public async Task CaptureAsync_SimpleField_UsesBaseNoInputTimeout(string valueTypeKind)
    {
        FormVoiceCommandCaptureSettings? receivedCaptureSettings = null;
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, captureSettings, onStartListening, _, _, _, _) =>
                {
                    receivedCaptureSettings = captureSettings;
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("7", 95));
                });

        var voiceSettings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 0
        };
        var (capture, turnContext) = CreateCapture(
            speechService.Object,
            valueTypeKind,
            options: [],
            voiceSettings);

        await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.NotNull(receivedCaptureSettings);
        Assert.Equal(10000, receivedCaptureSettings.VoiceSettings.NoInputTimeoutMs);
    }

    [Fact]
    public async Task CaptureAsync_SimpleFieldAfterOptionListingField_UsesBaseNoInputTimeout()
    {
        var receivedTimeouts = new List<int?>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, captureSettings, onStartListening, _, _, _, _) =>
                {
                    receivedTimeouts.Add(captureSettings.VoiceSettings.NoInputTimeoutMs);
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Blau", 95));
                });
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, captureSettings, onStartListening, _, _, _, _) =>
                {
                    receivedTimeouts.Add(captureSettings.VoiceSettings.NoInputTimeoutMs);
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("7", 95));
                });

        var voiceSettings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 0
        };
        var options = ImmutableArray.Create(
            new FormChoiceOption("c1", "Blau"),
            new FormChoiceOption("c2", "Rot"));
        var (capture, optionTurnContext) = CreateCapture(
            speechService.Object,
            ValueTypeKinds.Set,
            options,
            voiceSettings);
        var simpleTurnContext = CreateTurnContext(ValueTypeKinds.Integer, options: [], voiceSettings);

        await capture.CaptureAsync(optionTurnContext, InputMode.Voice, CancellationToken.None);
        await capture.CaptureAsync(simpleTurnContext, InputMode.Voice, CancellationToken.None);

        Assert.Equal([18000, 10000], receivedTimeouts);
    }

    private static (FormVoiceCommandFieldTranscriptCapture Capture, FormVoiceCommandFieldTurnContext TurnContext) CreateCapture(
        IFormVoiceCommandSpeechService speechService,
        string valueTypeKind,
        ImmutableArray<FormChoiceOption> options,
        VoiceDialogSettings? voiceSettings = null)
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object,
            new RecordingSpeechService(),
            new FormVoiceCommandSpokenOutputReplayBuffer(),
            new CaptureReplayPolicy());
        var capture = new FormVoiceCommandFieldTranscriptCapture(speechService, errorHandling);

        var session = new EvaluationSession("session-keyterms", EvaluationChannel.Voice, "app", "app/form");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            voiceSettings ?? new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            Mock.Of<IFormDialogRuntime>(),
            isoCode: "de");

        var turnContext = CreateTurnContext(valueTypeKind, options, voiceSettings, telemetry.Object, session, stateTracker, autoHelp);

        return (capture, turnContext);
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        string valueTypeKind,
        ImmutableArray<FormChoiceOption> options,
        VoiceDialogSettings? voiceSettings,
        IEvaluationTelemetry? telemetry = null,
        EvaluationSession? session = null,
        FormVoiceCommandStateTracker? stateTracker = null,
        FormVoiceCommandAutoHelpTracker? autoHelp = null)
    {
        telemetry ??= Mock.Of<IEvaluationTelemetry>();
        session ??= new EvaluationSession("session-keyterms", EvaluationChannel.Voice, "app", "app/form");
        stateTracker ??= new FormVoiceCommandStateTracker(telemetry, session);
        var field = CreateField(valueTypeKind, options);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        autoHelp ??= new FormVoiceCommandAutoHelpTracker(
            voiceSettings ?? new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            Mock.Of<IFormVoiceCommandDialogStateTracker>(),
            Mock.Of<IFormDialogRuntime>(),
            isoCode: "de");

        return new FormVoiceCommandFieldTurnContext(
            CreateContext(voiceSettings),
            Mock.Of<IFormDialogRuntime>(),
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            Mock.Of<IFormDialogPrivacyReadbackPolicy>());
    }

    private static FormFieldSnapshot CreateField(
        string valueTypeKind,
        ImmutableArray<FormChoiceOption> options)
    {
        return new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "favourite_colour",
                VariableId: "favourite_colour",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Lieblingsfarbe",
                HelpText: null,
                Examples: [],
                Options: options),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: valueTypeKind,
                SpeakValueInConfirmation: true));
    }

    private static FormVoiceCommandContext CreateContext(VoiceDialogSettings? voiceSettings = null)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("favourite_colour"),
                    Name = "favourite_colour",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("favourite_colour"),
                    VariableId = VariableId.From("favourite_colour"),
                    Label = new LocalizedText { ["de"] = "Lieblingsfarbe" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("favourite_colour") }
                    ]
                }
            ]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings ?? new VoiceDialogSettings { MaxRetries = 0 },
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
