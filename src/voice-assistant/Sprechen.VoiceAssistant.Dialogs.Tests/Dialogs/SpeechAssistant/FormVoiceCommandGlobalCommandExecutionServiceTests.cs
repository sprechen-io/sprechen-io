using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandGlobalCommandExecutionServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRouterReturnsDialogResult_TracksCommandAndReturnsDialogResult()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var service = new FormVoiceCommandGlobalCommandExecutionService(telemetry.Object, router.Object);

        var context = CreateContext();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/form");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "form",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        telemetry
            .Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.Is<FormVoiceCommandGlobalCommandContext>(commandContext =>
                    commandContext.CommandId == GlobalCommandIds.Next
                    && commandContext.Argument == "arg"
                    && commandContext.Session == session
                    && commandContext.Interpreter == interpreter.Object),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var result = await service.ExecuteAsync(
            context,
            interpreter.Object,
            session,
            stateTracker,
            $" {GlobalCommandIds.Next} ",
            argument: "arg",
            CancellationToken.None);

        Assert.Same(expectedResult, result.DialogResult);
        telemetry.VerifyAll();
        router.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRouterReturnsNull_ReturnsContinueResult()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var service = new FormVoiceCommandGlobalCommandExecutionService(telemetry.Object, router.Object);

        var context = CreateContext();
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var session = new EvaluationSession("session-2", EvaluationChannel.Voice, "app", "app/form");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        telemetry
            .Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry
            .Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry
            .Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry
            .Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Help));
        telemetry
            .Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Help),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandDialogResult?)null);

        stateTracker.BeginFieldPrompt("main");
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        var result = await service.ExecuteAsync(
            context,
            interpreter.Object,
            session,
            stateTracker,
            GlobalCommandIds.Help,
            argument: null,
            CancellationToken.None);

        Assert.Null(result.DialogResult);
        Assert.False(result.WasSubmitted);
        telemetry.VerifyAll();
        router.VerifyAll();
    }

    // Spalten: hasExplicitCommandPrefix, isLiteralMode, trimmedTranscript, parsedCommandId, expected.
    [Theory]
    // Ein explizites Befehlspräfix oder die konfigurierte Aktivierungsform erlaubt hier die Befehlsausführung.
    [InlineData(true, false, "foo", GlobalCommandIds.Help, true)]
    [InlineData(false, false, "okay guide help", GlobalCommandIds.Help, true)]
    [InlineData(false, false, "okay guide weiter", GlobalCommandIds.Next, true)]
    [InlineData(false, false, "okay guide abbrechen", GlobalCommandIds.Cancel, true)]
    [InlineData(false, false, "okay guide hilfe", GlobalCommandIds.Help, true)]
    [InlineData(false, false, "okay guide change language", GlobalCommandIds.ChangeLanguage, true)]
    [InlineData(false, false, "okay guide change politeness", GlobalCommandIds.ChangePoliteness, true)]
    // „okay“ allein entspricht keiner der in diesem Test konfigurierten Aktivierungsformen.
    [InlineData(false, false, "okay weiter", GlobalCommandIds.Next, false)]
    // Diese Befehlswörter bleiben ohne Aktivierungsform in diesem Kontext als Eingabe erhalten.
    [InlineData(false, false, "normaler freitext", GlobalCommandIds.Help, false)]
    [InlineData(false, false, "hilfe", GlobalCommandIds.Help, false)]
    [InlineData(false, false, "abbrechen", GlobalCommandIds.Cancel, false)]
    [InlineData(false, false, "normaler freitext", GlobalCommandIds.OpenApplications, false)]
    [InlineData(false, false, "normaler freitext", GlobalCommandIds.ChangeLanguage, false)]
    [InlineData(false, false, "wiederholen", GlobalCommandIds.Repeat, false)]
    [InlineData(false, false, "bitte wiederholen", GlobalCommandIds.Repeat, false)]
    [InlineData(false, false, "meine adresse ist wiederholen strasse 1", GlobalCommandIds.Repeat, false)]
    [InlineData(false, false, "weiter", GlobalCommandIds.Next, false)]
    [InlineData(false, false, "absenden", GlobalCommandIds.Submit, false)]
    [InlineData(false, false, "ich gehe weiter nach hause", GlobalCommandIds.Next, false)]
    [InlineData(false, false, "übernehmen", GlobalCommandIds.AcceptValue, false)]
    [InlineData(false, false, "ändern", GlobalCommandIds.EditValue, false)]
    // Außerhalb des wörtlichen Modus erlauben alleinstehende Modusbefehle den Wechsel auch ohne Aktivierungsform.
    // Ein in einen Satz eingebettetes „buchstabieren“ reicht dafür nicht aus.
    [InlineData(false, false, "buchstabieren an", GlobalCommandIds.InputModeSpelling, true)]
    [InlineData(false, false, "buchstabieren", GlobalCommandIds.InputModeSpelling, true)]
    [InlineData(false, false, "ziffernmodus", GlobalCommandIds.InputModeDigits, true)]
    [InlineData(false, false, "spracheingabe", GlobalCommandIds.InputModeVoice, true)]
    [InlineData(false, false, "tastatureingabe", GlobalCommandIds.InputModeKeyboard, true)]
    [InlineData(false, false, "wörtlich", GlobalCommandIds.InputModeLiteral, true)]
    [InlineData(false, false, "ich moechte das jetzt buchstabieren bitte", GlobalCommandIds.InputModeSpelling, false)]
    // Im wörtlichen Modus bleiben gewöhnliche Befehlswörter ohne Aktivierungsform Eingabe.
    // Der alleinstehende Befehl zum Beenden dieses Modus bleibt als Ausnahme ausführbar.
    [InlineData(false, true, "weiter", GlobalCommandIds.Next, false)]
    [InlineData(false, true, "okay guide weiter", GlobalCommandIds.Next, true)]
    [InlineData(false, true, "abbrechen", GlobalCommandIds.Cancel, false)]
    [InlineData(false, true, "okay guide abbrechen", GlobalCommandIds.Cancel, true)]
    [InlineData(false, true, "buchstabieren", GlobalCommandIds.InputModeSpelling, false)]
    [InlineData(false, true, "literal aus", GlobalCommandIds.InputModeLiteralOff, true)]
    [InlineData(false, true, "okay guide literal aus", GlobalCommandIds.InputModeLiteralOff, true)]
    [InlineData(false, true, "ich sage literal aus zum spass", GlobalCommandIds.InputModeLiteralOff, false)]
    public void ShouldExecuteParsedCommand_FollowsExpectedRules(
        bool hasExplicitCommandPrefix,
        bool isLiteralMode,
        string trimmedTranscript,
        string parsedCommandId,
        bool expected)
    {
        var context = CreateContext();

        var result = FormVoiceCommandCommandSemantics.ShouldExecuteParsedCommand(
            context.Contract,
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel(),
            hasExplicitCommandPrefix,
            isLiteralMode,
            trimmedTranscript,
            parsedCommandId,
            ["okay guide", "ok guide"]);

        Assert.Equal(expected, result);
    }

    private static FormVoiceCommandContext CreateContext()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            Description = new LocalizedText { ["de"] = "Form" },
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
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
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
    }
}
