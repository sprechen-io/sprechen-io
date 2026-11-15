using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Reaktion auf nicht konvergierende Initialisierungsregeln.
/// Die Laufzeit meldet den Fehler an die Sprachausgabe-Schnittstelle und die Navigationstelemetrie und stellt den vorherigen Feldwert wieder her.
/// Konvergierende Regeln dürfen diese Fehlermeldungen nicht auslösen.
/// </summary>
public sealed class FormRuleLoopGuardEmissionTests
{
    [Fact]
    public async Task LoopGuardBreach_OnNonConvergingOnInitRules_SurfacesErrorCodeAudiblyAndEmitsNavigationErrorTelemetry()
    {
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/current");
        var formTelemetry = new Mock<IEvaluationFormStatusTelemetry>();
        var captureFeedback = new Mock<ISpeechCaptureFeedback>();
        var loopGuardFeedback = new FormRuleLoopGuardFeedback(formTelemetry.Object, captureFeedback.Object, session);

        // Die Initialisierungsregeln wechseln den Wert ohne Konvergenz bereits beim Aufbau der Laufzeit.
        var runtime = CreateRuntimeOwner(
            CreateNonConvergingRulesContract(),
            loopGuardFeedback,
            session.SessionId,
            ImmutableDictionary<string, string>.Empty.Add("flag", "false"));

        // Den modellierten Fehlercode an die Schnittstelle für Erfassungsfehler melden.
        captureFeedback.Verify(
            feedback => feedback.ReportFailureAsync(
                SpeechErrorCode.FormRuleLoopGuardExceeded,
                RuntimeErrorCodes.RuleLoopMaxIterationsExceeded),
            Times.Once);

        // Die Navigationstelemetrie erhält Fehlerklasse und Geltungsbereich ohne Feldwerte.
        formTelemetry.Verify(
            telemetry => telemetry.TrackNavigationError(session, NavigationErrorCodes.LostContext, ConstraintScopes.CrossGroup),
            Times.Once);

        // Der Feldwert muss auf dem Stand vor der Regelausführung bleiben.
        var values = await runtime.GetValuesSnapshotAsync();
        Assert.Equal("false", values["flag"]);
    }

    [Fact]
    public void LoopGuardBreach_SignalsThroughTheProductionRuntimeFactory()
    {
        // Auch die produktive Laufzeitfabrik muss den übergebenen Empfänger für Regelabbrüche anbinden.
        var loopGuardFeedback = new Mock<IFormRuleLoopGuardFeedback>();
        var context = new Mock<IFormDialogRuntimeContext>();
        context.SetupGet(item => item.IsoCode).Returns("de");
        context.SetupGet(item => item.Contract).Returns(CreateNonConvergingRulesContract());
        context.SetupGet(item => item.VoiceSettings).Returns(new VoiceDialogSettings());
        context.SetupGet(item => item.ValuesByVariableId).Returns(ImmutableDictionary<string, string>.Empty);

        var factory = new FormDialogRuntimeFactory(CreateTranscriptParser());

        _ = factory.Create(context.Object, "session-1", loopGuardFeedback.Object);

        loopGuardFeedback.Verify(feedback => feedback.OnRuleLoopGuardBreach(), Times.Once);
    }

    [Fact]
    public void ConvergingOnInitRules_EmitNoLoopGuardSignals()
    {
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/current");
        var formTelemetry = new Mock<IEvaluationFormStatusTelemetry>();
        var captureFeedback = new Mock<ISpeechCaptureFeedback>();
        var loopGuardFeedback = new FormRuleLoopGuardFeedback(formTelemetry.Object, captureFeedback.Object, session);

        _ = CreateRuntimeOwner(CreateConvergingRulesContract(), loopGuardFeedback, session.SessionId);

        // Konvergierende Regeln dürfen weder den Fehlerempfänger noch die Navigationstelemetrie aufrufen.
        captureFeedback.Verify(
            feedback => feedback.ReportFailureAsync(It.IsAny<SpeechErrorCode>(), It.IsAny<string>()),
            Times.Never);
        formTelemetry.Verify(
            telemetry => telemetry.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    private static FormInterpreterRuntimeOwner CreateRuntimeOwner(
        FormContract contract,
        IFormRuleLoopGuardFeedback loopGuardFeedback,
        string sessionId,
        ImmutableDictionary<string, string>? initialValues = null)
        => new(
            contract,
            "de",
            initialValues ?? ImmutableDictionary<string, string>.Empty,
            CreateTranscriptParser(),
            PolitenessLevel.Formal,
            sessionId,
            loopGuardFeedback);

    private static IFormTranscriptParser CreateTranscriptParser()
        => FormTranscriptParsers.CreateDefault(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

    private static FormContract CreateNonConvergingRulesContract()
        => CreateContractWithRules(
        [
            new RuleDefinition
            {
                RuleId = "r-oscillate",
                Priority = 0,
                Scope = RuleScopes.OnInit,
                When = ParseJson("""{"kind":"equals","left":{"kind":"var","variableId":"flag"},"right":{"kind":"const","value":true}}"""),
                Then = [ParseJson("""{"kind":"setValue","variableId":"flag","value":false}""")],
                Else = [ParseJson("""{"kind":"setValue","variableId":"flag","value":true}""")]
            }
        ]);

    private static FormContract CreateConvergingRulesContract()
        => CreateContractWithRules(
        [
            new RuleDefinition
            {
                RuleId = "r-converge",
                Priority = 0,
                Scope = RuleScopes.OnInit,
                When = ParseJson("""{"kind":"equals","left":{"kind":"const","value":1},"right":{"kind":"const","value":1}}"""),
                Then = [ParseJson("""{"kind":"setValue","variableId":"flag","value":false}""")]
            }
        ]);

    private static FormContract CreateContractWithRules(List<RuleDefinition> rules)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("flag"),
                    Name = "flag",
                    ValueType = JsonDocument.Parse("""{"kind":"boolean"}""").RootElement
                },
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
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }
                    ]
                }
            ],
            Rules = rules
        };
    }

    private static JsonElement ParseJson(string json)
        => JsonDocument.Parse(json).RootElement;
}
