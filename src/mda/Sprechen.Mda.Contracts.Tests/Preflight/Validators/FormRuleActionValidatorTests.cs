using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests.Preflight.Validators;

public sealed class FormRuleActionValidatorTests
{
    [Fact]
    public void Validate_WhenActionIsNotObject_ReturnsFeatureNotSupported()
    {
        var errors = new List<PreflightIssue>();

        new FormRuleActionValidator().Validate(ParseJsonElement("[]"), "$.rules[0].then[0]", CreateIndex(), errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.rules[0].then[0]");
    }

    [Fact]
    public void Validate_WhenUnsupportedActionKind_ReturnsFeatureNotSupported()
    {
        var errors = new List<PreflightIssue>();

        new FormRuleActionValidator().Validate(
            ParseJsonElement("""{"kind":"customAction","customActionName":"x"}"""),
            "$.rules[0].then[0]",
            CreateIndex(),
            errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.rules[0].then[0].kind");
    }

    [Fact]
    public void Validate_WhenSetVisibleTargetsMissingFieldGroup_ReturnsRefMissing()
    {
        var errors = new List<PreflightIssue>();

        new FormRuleActionValidator().Validate(
            ParseJsonElement("""{"kind":"setVisible","target":{"kind":"fieldGroup","fieldGroupId":"missing"},"value":true}"""),
            "$.rules[0].then[0]",
            CreateIndex(),
            errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.rules[0].then[0].target.fieldGroupId"
            && issue.Id == "missing");
    }

    [Fact]
    public void Validate_WhenCopyValueReferencesUnknownVariables_ReturnsBothErrors()
    {
        var errors = new List<PreflightIssue>();

        new FormRuleActionValidator().Validate(
            ParseJsonElement("""{"kind":"copyValue","fromVariableId":"unknownFrom","toVariableId":"unknownTo"}"""),
            "$.rules[0].then[0]",
            CreateIndex(),
            errors);

        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.rules[0].then[0].fromVariableId"
            && issue.Id == "unknownFrom");
        Assert.Contains(errors, issue =>
            issue.Code == PreflightCodes.ErrorRefMissing
            && issue.Path == "$.rules[0].then[0].toVariableId"
            && issue.Id == "unknownTo");
    }

    [Fact]
    public void Validate_WhenSpeakReferencesKnownPrompt_AllowsAction()
    {
        var errors = new List<PreflightIssue>();

        new FormRuleActionValidator().Validate(
            ParseJsonElement("""{"kind":"speak","promptId":"prompt.main"}"""),
            "$.rules[0].then[0]",
            CreateIndex(),
            errors);

        Assert.Empty(errors);
    }

    private static FormContractPreflightIndex CreateIndex()
    {
        var variable = new VariableDefinition
        {
            VariableId = VariableId.From("name"),
            Name = "Name",
            ValueType = ParseJsonElement("""{"kind":"string"}""")
        };
        var field = new FieldDefinition
        {
            FieldId = FieldId.From("nameField"),
            VariableId = VariableId.From("name"),
            Label = new LocalizedText { ["de"] = "Name" }
        };
        var fieldGroup = new FieldGroupDefinition
        {
            FieldGroupId = FieldGroupId.From("main"),
            Title = new LocalizedText { ["de"] = "Main" }
        };

        return new FormContractPreflightIndex(
            new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
            {
                [variable.VariableId] = variable
            },
            new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
            {
                [field.FieldId] = field
            },
            new Dictionary<string, FieldGroupDefinition>(StringComparer.Ordinal)
            {
                [fieldGroup.FieldGroupId] = fieldGroup
            },
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { "prompt.main" });
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
