using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Prüft den Abbruchbefehl während einer Feldbestätigung über Ausführungsdienst,
// Router und Abbruchhandler. Die Ja-Nein-Entscheidung wird simuliert;
// Spracherkennung, Aktivierungswort und tatsächliche Wiedergabe sind nicht Teil des Tests.
public sealed class FormVoiceCommandConfirmationTurnCancelExecutionTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAnchoredCancelDeclinedDuringConfirmation_AsksSafetyQuestionAndDoesNotCancel()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var fixture = CreateFixture(telemetry.Object, speechService.Object);
        DriveToFieldConfirmation(fixture.StateTracker);

        var result = await fixture.Service.ExecuteAsync(
            fixture.Context,
            fixture.Interpreter,
            fixture.Session,
            fixture.StateTracker,
            GlobalCommandIds.Cancel,
            argument: null,
            CancellationToken.None);

        speechService.Verify(service => service.ConfirmYesNoAsync(
            It.IsAny<FormVoiceCommandCaptureSettings>(),
            It.IsAny<string>(),
            "de",
            It.IsAny<SpeechCaptureTelemetryContext?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.Verify(t => t.TrackDestructiveCommandResolved(
            fixture.Session,
            GlobalCommandIds.Cancel,
            false,
            false), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnchoredCancelConfirmedDuringConfirmation_AsksSafetyQuestionThenCancels()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(service => service.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var fixture = CreateFixture(telemetry.Object, speechService.Object);
        DriveToFieldConfirmation(fixture.StateTracker);

        var result = await fixture.Service.ExecuteAsync(
            fixture.Context,
            fixture.Interpreter,
            fixture.Session,
            fixture.StateTracker,
            GlobalCommandIds.Cancel,
            argument: null,
            CancellationToken.None);

        speechService.Verify(service => service.ConfirmYesNoAsync(
            It.IsAny<FormVoiceCommandCaptureSettings>(),
            It.IsAny<string>(),
            "de",
            It.IsAny<SpeechCaptureTelemetryContext?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(result.DialogResult);
        Assert.True(result.DialogResult.WasCancelled);
        telemetry.Verify(t => t.TrackDestructiveCommandResolved(
            fixture.Session,
            GlobalCommandIds.Cancel,
            true,
            false), Times.Once);
    }

    private static void DriveToFieldConfirmation(FormVoiceCommandStateTracker stateTracker)
    {
        stateTracker.BeginFieldPrompt("main");
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
        stateTracker.BeginValidation();
        stateTracker.BeginFieldConfirmation();
    }

    private static FormVoiceCommandConfirmationTurnCancelExecutionFixture CreateFixture(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        var contract = CreateContract();
        var form = CreateForm();
        var application = CreateApplication(form);
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/form");

        var clock = new MutableClock(new DateTimeOffset(2026, 2, 8, 15, 0, 0, TimeSpan.Zero));
        var cancelHandler = new FormVoiceCommandCancelHandler(clock, telemetry, speechService);
        var router = new FormVoiceCommandGlobalCommandRouter([cancelHandler]);
        var service = new FormVoiceCommandGlobalCommandExecutionService(telemetry, router);

        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary<string, string>.Empty);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);

        var context = new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));

        return new FormVoiceCommandConfirmationTurnCancelExecutionFixture(service, context, interpreter, session, stateTracker);
    }

    private static FormDescriptor CreateForm()
    {
        return new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
    }

    private static ApplicationDescriptor CreateApplication(FormDescriptor form)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };
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
