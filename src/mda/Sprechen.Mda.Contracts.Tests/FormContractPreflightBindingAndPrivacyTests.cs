using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractPreflightBindingAndPrivacyTests
{
    [Fact]
    public void Validate_WhenDateFormatHintUsesStringValueType_AllowsPresentationOnlyHint()
    {
        var contract = CreateContract(
            variableId: "birthDate",
            fieldId: "birthDateField",
            valueTypeJson: """{"kind":"string","formatHint":"date"}""");

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenSensitiveFieldUsesSpellOutConfirmationStyle_ReturnsPrivacyGuardrailViolation()
    {
        var contract = CreateContract(
            variableId: "iban",
            fieldId: "ibanField",
            valueTypeJson: """{"kind":"string","formatHint":"iban"}""",
            codeStyle: true,
            privacyClass: PrivacyClass.Sensitive,
            confirmationStyle: "spellOut");

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "ibanField"
            && issue.Path == "$.fields[*].projections.voice.confirmationStyle");
    }

    [Fact]
    public void Validate_WhenUnknownPrivacyClassIsPresent_ReturnsPrivacyGuardrailViolation()
    {
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Unknown);

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "name"
            && issue.Path == "$.variables[*].privacyClass");
    }

    [Fact]
    public void Validate_WhenSecretVariableAllowsVoiceInput_ReturnsPrivacyGuardrailViolation()
    {
        var contract = CreateContract(
            variableId: "pin",
            fieldId: "pinField",
            valueTypeJson: """{"kind":"string","formatHint":"pin"}""",
            codeStyle: true,
            privacyClass: PrivacyClass.Secret,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = "recommended"
            });

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "pin"
            && issue.Path == "$.variables[*].sensitiveDataPolicy.voiceInputPolicy");
    }

    [Fact]
    public void Validate_WhenVoiceHintsSpeechRateIsOutOfRange_ReturnsSettingsInvalid()
    {
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string","formatHint":"text"}""",
            formSpeechRate: 3.0);

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorSettingsInvalid
            && issue.Path == "$.projections.voice.formVoiceProfile.speechRate");
    }

    [Theory]
    [InlineData("public", true)]
    [InlineData("personal", true)]
    [InlineData("sensitive", false)]
    [InlineData("secret", false)]
    public void Validate_RepeatBackConfirmationStyle_AcceptsOnlyPublicAndPersonal(string privacyClassToken, bool expectedOk)
    {
        Assert.True(PrivacyClassExtensions.TryFromWireToken(privacyClassToken, out var privacyClass));
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: privacyClass,
            confirmationStyle: "repeatBack");

        var result = FormContractPreflight.Validate(contract);

        Assert.Equal(expectedOk, result.Ok);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("personal")]
    public void Validate_SpellOutConfirmationStyle_AcceptsPublicAndPersonal(string privacyClassToken)
    {
        Assert.True(PrivacyClassExtensions.TryFromWireToken(privacyClassToken, out var privacyClass));
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: privacyClass,
            confirmationStyle: "spellOut");

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Validate_WhenSecretFieldUsesSpellOutConfirmationStyle_ReturnsPrivacyGuardrailViolation()
    {
        var contract = CreateContract(
            variableId: "pin",
            fieldId: "pinField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Secret,
            confirmationStyle: "spellOut");

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "pinField"
            && issue.Path == "$.fields[*].projections.voice.confirmationStyle");
    }

    [Theory]
    [InlineData("public")]
    [InlineData("personal")]
    [InlineData("sensitive")]
    [InlineData("secret")]
    public void Validate_DefaultConfirmationStyle_AcceptsEveryPrivacyClassWithWithheldSpeech(string privacyClassToken)
    {
        // Der Standardstil wird für alle Datenschutzklassen akzeptiert; die Regeln für
        // Spracheingabe und Wiedergabe werden unabhängig davon geprüft.
        Assert.True(PrivacyClassExtensions.TryFromWireToken(privacyClassToken, out var privacyClass));
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: privacyClass,
            confirmationStyle: "default");

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("summarize")]
    public void Validate_NoneAndSummarizeConfirmationStyles_AreNotPrivacyRestricted(string confirmationStyle)
    {
        var contract = CreateContract(
            variableId: "name",
            fieldId: "nameField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Sensitive,
            confirmationStyle: confirmationStyle);

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Validate_WhenSensitiveVariableRequiresAlwaysReadback_ReturnsPrivacyGuardrailViolation()
    {
        var contract = CreateContract(
            variableId: "iban",
            fieldId: "ibanField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Sensitive,
            confirmationStyle: "default",
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackAlways
            });

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, issue =>
            issue.Code == PreflightCodes.ErrorPrivacyGuardrailViolation
            && issue.Id == "iban"
            && issue.Path == "$.variables[*].sensitiveDataPolicy.readbackPolicy");
    }

    [Fact]
    public void Validate_WhenSecretVariableWithholdsSpeech_IsAccepted()
    {
        // Für geheime Daten ist die Kombination aus verbotener Spracheingabe
        // und ausgeschlossener Wiedergabe zulässig.
        var contract = CreateContract(
            variableId: "pin",
            fieldId: "pinField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Secret,
            confirmationStyle: "default",
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
            });

        var result = FormContractPreflight.Validate(contract);

        Assert.True(result.Ok);
    }

    [Fact]
    public void Validate_WhenMultiplePrivacyViolationsExist_ReportsThemInDeterministicContractOrder()
    {
        // Zwei fehlerhafte Variablen und ein unzulässiger Bestätigungsstil
        // müssen bei jedem Lauf in derselben vorgegebenen Reihenfolge gemeldet werden.
        var contract = CreateContract(
            variableId: "first",
            fieldId: "firstField",
            valueTypeJson: """{"kind":"string"}""",
            privacyClass: PrivacyClass.Secret,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputRecommended
            },
            confirmationStyle: "repeatBack");
        contract.Variables.Add(new VariableDefinition
        {
            VariableId = VariableId.From("second"),
            Name = "second",
            PrivacyClass = PrivacyClass.Secret,
            SensitiveDataPolicy = new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputOptional
            },
            ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
        });
        contract.Fields.Add(new FieldDefinition
        {
            FieldId = FieldId.From("secondField"),
            VariableId = VariableId.From("second"),
            Label = new LocalizedText { ["de"] = "Zweitfeld" },
            Editability = FieldEditability.UserEditable
        });
        contract.FieldGroups[0].Members.Add(new FieldGroupMember { Kind = "field", FieldId = FieldId.From("secondField") });

        var first = FormContractPreflight.Validate(contract);
        var second = FormContractPreflight.Validate(contract);

        Assert.False(first.Ok);
        Assert.Equal(
            first.Errors.Select(issue => (issue.Code, issue.Id, issue.Path)).ToArray(),
            second.Errors.Select(issue => (issue.Code, issue.Id, issue.Path)).ToArray());
        Assert.Equal(
            [
                (PreflightCodes.ErrorPrivacyGuardrailViolation, "first", "$.variables[*].sensitiveDataPolicy.voiceInputPolicy"),
                (PreflightCodes.ErrorPrivacyGuardrailViolation, "firstField", "$.fields[*].projections.voice.confirmationStyle"),
                (PreflightCodes.ErrorPrivacyGuardrailViolation, "second", "$.variables[*].sensitiveDataPolicy.voiceInputPolicy")
            ],
            first.Errors.Select(issue => (issue.Code, issue.Id, issue.Path)).ToArray());
    }

    private static FormContract CreateContract(
        string variableId,
        string fieldId,
        string valueTypeJson,
        bool codeStyle = false,
        PrivacyClass privacyClass = PrivacyClass.Personal,
        string? confirmationStyle = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null,
        double? formSpeechRate = null)
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
            Projections = formSpeechRate is null
                ? null
                : new ContractProjections
                {
                    Voice = new VoiceProjection
                    {
                        FormVoiceProfile = new FormVoiceProfile
                        {
                            SpeechRate = formSpeechRate
                        }
                    }
                },
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From(variableId),
                    Name = variableId,
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = sensitiveDataPolicy,
                    ValueType = ParseJsonElement(valueTypeJson)
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From(fieldId),
                    VariableId = VariableId.From(variableId),
                    Label = new LocalizedText { ["de"] = fieldId },
                    Editability = FieldEditability.UserEditable,
                    CodeStyle = codeStyle,
                    Projections = confirmationStyle is null
                        ? null
                        : new FieldProjections
                        {
                            Voice = new FieldVoiceProjection
                            {
                                ConfirmationStyle = confirmationStyle
                            }
                        }
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From(fieldId) }]
                }
            ]
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}


