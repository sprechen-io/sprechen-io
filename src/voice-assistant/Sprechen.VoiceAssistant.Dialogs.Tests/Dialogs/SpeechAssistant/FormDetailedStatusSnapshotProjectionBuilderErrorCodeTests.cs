using System.Collections.Immutable;
using System.Text.Json;
using FieldIdKey = Sprechen.Mda.Contracts.V1.Identifiers.FieldId;
using VariableIdKey = Sprechen.Mda.Contracts.V1.Identifiers.VariableId;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass die Fehlerlisten des detaillierten Status den fachlichen Fehlercode tragen.
/// Die Regelkennung ist absichtlich anders gewählt, damit eine Verwechslung im Test auffällt.
/// </summary>
public sealed class FormDetailedStatusSnapshotProjectionBuilderErrorCodeTests
{
    private const string FieldId = "nameField";
    private const string FieldGroupId = "main";

    [Fact]
    public void BuildDetailedStatusSnapshot_FieldScopedError_BlockingErrorCodesCarrySemanticCode()
    {
        var builder = CreateBuilder();

        var snapshot = builder.BuildDetailedStatusSnapshot(
            new Dictionary<string, string>(StringComparer.Ordinal),
            [CreateViolation(constraintId: "nameRuleInstance", code: "form.test.fieldcode", ConstraintScopes.Field)],
            _ => true);

        var field = Assert.Single(snapshot.Fields, f => f.FieldId == FieldId);
        Assert.Contains("form.test.fieldcode", field.BlockingErrorCodes);
        Assert.DoesNotContain("nameRuleInstance", field.BlockingErrorCodes);
    }

    [Fact]
    public void BuildDetailedStatusSnapshot_GroupScopedError_OpenGroupErrorCodesCarrySemanticCode()
    {
        var builder = CreateBuilder();

        var snapshot = builder.BuildDetailedStatusSnapshot(
            new Dictionary<string, string>(StringComparer.Ordinal),
            [CreateViolation(constraintId: "groupRuleInstance", code: "form.test.groupcode", ConstraintScopes.FieldGroup)],
            _ => true);

        var group = Assert.Single(snapshot.FieldGroups, g => g.FieldGroupId == FieldGroupId);
        Assert.Contains("form.test.groupcode", group.OpenGroupErrorCodes);
        Assert.DoesNotContain("groupRuleInstance", group.OpenGroupErrorCodes);
    }

    private static FormDetailedStatusSnapshotProjectionBuilder CreateBuilder()
    {
        var fieldToFieldGroupId = ImmutableDictionary<FieldIdKey, string>.Empty.Add(FieldIdKey.From(FieldId), FieldGroupId);
        var fieldCompletionService = new FormFieldCompletionPolicy(
            ImmutableDictionary<FieldIdKey, FieldDefinition>.Empty.Add(
                FieldIdKey.From(FieldId),
                new FieldDefinition { FieldId = Sprechen.Mda.Contracts.V1.Identifiers.FieldId.From(FieldId), VariableId = VariableId.From("name") }),
            ImmutableDictionary<FieldIdKey, string>.Empty.Add(FieldIdKey.From(FieldId), "name"),
            ImmutableDictionary<VariableIdKey, VariableDefinition>.Empty.Add(
                VariableIdKey.From("name"),
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement
                }),
            ImmutableArray<ConstraintDefinition>.Empty);
        var fieldGroupCompletionService = new FormFieldGroupCompletionService(
            fieldToFieldGroupId,
            ImmutableArray<ConstraintDefinition>.Empty,
            fieldCompletionService);
        var projectionScopeResolver = new FormDialogProjectionScopeResolver(
            [FieldGroupId],
            [FieldId],
            fieldToFieldGroupId);

        return new FormDetailedStatusSnapshotProjectionBuilder(
            fieldToFieldGroupId,
            projectionScopeResolver,
            fieldCompletionService,
            fieldGroupCompletionService,
            new FormErrorViolationFilter());
    }

    private static FormConstraintViolation CreateViolation(string constraintId, string code, string scope)
        => new(
            FieldId,
            constraintId,
            SeverityLevels.Error,
            code,
            Message: code,
            Remediation: null,
            scope,
            FieldGroupId);
}
