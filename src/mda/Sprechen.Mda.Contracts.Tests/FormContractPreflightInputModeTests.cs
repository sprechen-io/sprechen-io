using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractPreflightInputModeTests
{
    [Fact]
    public void Validate_WhenPreferredInputModeUnsupported_ReturnsFeatureNotSupportedError()
    {
        var contract = CreateMinimalContract();
        contract.Fields[0].PreferredInputModes = ["charByChar"];

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Path == "$.fields[0].preferredInputModes[0]");
    }

    [Fact]
    public void Validate_WhenPreferredInputModeSpelling_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Fields[0].PreferredInputModes = ["spelling"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("voice")]
    [InlineData("spelling")]
    [InlineData("digits")]
    [InlineData("literal")]
    public void Validate_WhenSecretFieldPrefersAMicrophoneMode_ReturnsPrivacyGuardrailViolation(string microphoneMode)
    {
        // Geheime Felder dürfen keinen Eingabemodus bevorzugen, der eine Mikrofonaufnahme benötigt.
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        contract.Fields[0].PreferredInputModes = ["keyboard", microphoneMode];

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "nameField");
    }

    [Fact]
    public void Validate_WhenSecretFieldPrefersOnlyKeyboard_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        contract.Fields[0].PreferredInputModes = ["keyboard"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenSensitiveFieldPrefersSpellingWithOptInReadback_IsAccepted()
    {
        // Ein sensibles Feld darf Buchstabieren bevorzugen, während seine
        // Wiedergaberegel eine ausdrückliche Zustimmung verlangt.
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired
        };
        contract.Fields[0].PreferredInputModes = ["spelling", "keyboard"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenSensitiveFieldPrefersSpellingWithoutReadbackOptIn_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
        };
        contract.Fields[0].PreferredInputModes = ["spelling", "keyboard"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenSensitiveFieldPrefersVoice_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;
        contract.Fields[0].PreferredInputModes = ["voice", "keyboard"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenPersonalFieldPrefersSpelling_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Personal;
        contract.Fields[0].PreferredInputModes = ["spelling", "voice"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenFieldPrefersDigitsOnPlainText_ReturnsTypeIncompatibleError()
    {
        // Zifferneingabe bei einfachem Text ohne passenden Formathinweis
        // gehört nicht zu den zulässigen Kombinationen.
        var contract = CreateMinimalContract();
        contract.Fields[0].PreferredInputModes = ["digits", "voice"];

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Id == "nameField");
    }

    [Fact]
    public void Validate_WhenFieldPrefersDigitsWithDigitFriendlyHint_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].ValueType = JsonDocument.Parse("""{"kind":"string","formatHint":"phone"}""").RootElement;
        contract.Fields[0].PreferredInputModes = ["digits", "voice"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenFieldPrefersSpellingOnBoolean_ReturnsTypeIncompatibleError()
    {
        var contract = CreateMinimalContract();
        contract.Variables[0].ValueType = JsonDocument.Parse("""{"kind":"boolean"}""").RootElement;
        contract.Fields[0].PreferredInputModes = ["spelling", "voice"];

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorFeatureNotSupported
            && issue.Id == "nameField");
    }

    [Fact]
    public void Validate_WhenFieldPrefersLiteralOnText_IsAccepted()
    {
        var contract = CreateMinimalContract();
        contract.Fields[0].PreferredInputModes = ["literal", "voice"];

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    private static FormContract CreateMinimalContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable,
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }]
                }
            ]
        };
    }
}


