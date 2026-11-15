using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class TaskCardMismatchAssessorTests
{
    [Fact]
    public void TechnicalFailure_OutranksEveryCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.NotAssessed,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 0,
                requiredTotal: 1,
                validationErrors: 2,
                TelemetryTaskTerminationClassifications.TechnicalFailure));
    }

    [Fact]
    public void StudyDirectedAbort_OutranksEveryCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.NotAssessed,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.StudyDirectedAbort));
    }

    [Fact]
    public void MissingRequired_OutranksATaskCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.MissingRequired,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 0,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed));
    }

    [Fact]
    public void WrongFieldClass_OutranksATaskCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.WrongFieldClass,
            Assess(
                actual: "green",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed,
                choice: true));
    }

    [Fact]
    public void WrongDependencyState_OutranksATaskCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.WrongDependencyState,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed,
                answerRelevant: false));
    }

    [Fact]
    public void OpenValidationError_OutranksATaskCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.ValidationErrorOpen,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 1,
                TelemetryTaskTerminationClassifications.Completed));
    }

    [Fact]
    public void MatchingCardThatWasNotSubmitted_IsNotSubmitted()
    {
        Assert.Equal(
            TelemetryMismatchCodes.NotSubmitted,
            Assess(
                actual: "Wander Pokal",
                processSubmitted: false,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.ParticipantCancelled));
    }

    [Fact]
    public void FreeTextMismatch_IsTaskCardMismatch()
    {
        Assert.Equal(
            TelemetryMismatchCodes.TaskCardMismatch,
            Assess(
                actual: "anderer Preis",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed));
    }

    [Fact]
    public void MatchingFreeTextCard_IsNone()
    {
        Assert.Equal(
            TelemetryMismatchCodes.None,
            Assess(
                actual: "Wander Pokal",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed));
    }

    [Fact]
    public void JoinedSetOfCodes_MatchesDisplayNameTarget()
    {
        Assert.Equal(
            TelemetryMismatchCodes.None,
            Assess(
                actual: "musik,weltraum,geschichte",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed,
                choice: true,
                set: true));
    }

    [Fact]
    public void BooleanYesProse_MapsOntoTrue()
    {
        Assert.Equal(
            TelemetryMismatchCodes.None,
            Assess(
                actual: "true",
                processSubmitted: true,
                requiredCompleted: 1,
                requiredTotal: 1,
                validationErrors: 0,
                TelemetryTaskTerminationClassifications.Completed,
                booleanField: true));
    }

    [Fact]
    public void RuntimeNeverEmitsAbortedOrUnclear()
    {
        foreach (var code in new[]
        {
            Assess("anderer Preis", true, 1, 1, 0, TelemetryTaskTerminationClassifications.Completed),
            Assess("Wander Pokal", true, 1, 1, 0, TelemetryTaskTerminationClassifications.Completed),
            Assess("Wander Pokal", false, 1, 1, 0, TelemetryTaskTerminationClassifications.ParticipantCancelled),
            Assess("anderer Preis", true, 0, 1, 0, TelemetryTaskTerminationClassifications.Completed),
            Assess("anderer Preis", true, 1, 1, 0, TelemetryTaskTerminationClassifications.TechnicalFailure),
        })
        {
            Assert.NotEqual(TelemetryMismatchCodes.Aborted, code);
            Assert.NotEqual(TelemetryMismatchCodes.Unclear, code);
        }
    }

    private static string Assess(
        string actual,
        bool processSubmitted,
        int requiredCompleted,
        int requiredTotal,
        int validationErrors,
        string terminationClassification,
        bool choice = false,
        bool answerRelevant = true,
        bool set = false,
        bool booleanField = false)
    {
        var contract = CreateCardContract(choice, set, booleanField);
        var values = ImmutableDictionary<string, string>.Empty.Add("answer", actual);
        var relevant = answerRelevant
            ? new HashSet<string>(StringComparer.Ordinal) { "instruction-field", "answer-field" }
            : new HashSet<string>(StringComparer.Ordinal) { "instruction-field" };
        return TaskCardMismatchAssessor.Assess(
            contract,
            values,
            relevant,
            processSubmitted,
            requiredTotal,
            requiredCompleted,
            validationErrors,
            terminationClassification);
    }

    private static FormContract CreateCardContract(bool choice, bool set = false, bool booleanField = false)
    {
        var target = booleanField
            ? JsonDocument.Parse("\"ja, die Siegerehrung findet statt\"").RootElement
            : set
                ? JsonDocument.Parse("\"Musik, Weltraum, Geschichte\"").RootElement
                : JsonDocument.Parse("\"Wander-Pokal\"").RootElement;
        var valueType = booleanField
            ? JsonDocument.Parse("""{"kind":"boolean"}""").RootElement
            : set
                ? JsonDocument.Parse("""{"kind":"set","values":[{"code":"musik","displayName":{"de":"Musik"}},{"code":"weltraum","displayName":{"de":"Weltraum"}},{"code":"geschichte","displayName":{"de":"Geschichte"}}]}""").RootElement
            : choice
                ? JsonDocument.Parse("""{"kind":"string","values":[{"code":"red"},{"code":"blue"}]}""").RootElement
                : JsonDocument.Parse("""{"kind":"string"}""").RootElement;
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("card"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("instruction"),
                    Name = "instruction",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement,
                    InitialValue = target
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("answer"),
                    Name = "answer",
                    ValueType = valueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("instruction-field"),
                    VariableId = VariableId.From("instruction"),
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("answer-field"),
                    VariableId = VariableId.From("answer"),
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("card"),
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("instruction-field") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("answer-field") }
                    ]
                }
            ]
        };
    }
}
