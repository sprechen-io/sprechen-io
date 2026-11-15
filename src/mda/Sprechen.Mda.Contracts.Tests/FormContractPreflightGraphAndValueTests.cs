using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormContractPreflightGraphAndValueTests
{
    [Fact]
    public void Validate_WhenFieldGroupsFormCycle_ReturnsFieldGroupCycleError()
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("groupA"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
                    FieldGroupId = FieldGroupId.From("groupA"),
                    Title = new LocalizedText { ["de"] = "Gruppe A" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") },
                        new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("groupB") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("groupB"),
                    Title = new LocalizedText { ["de"] = "Gruppe B" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("groupA") }
                    ]
                }
            ]
        };

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorFieldGroupCycle);
    }

    [Fact]
    public void Validate_WhenFieldGroupNotReachableFromEntry_ReturnsFieldGroupUnreachableError()
    {
        var contract = new FormContract
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
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("orphan"),
                    Title = new LocalizedText { ["de"] = "Orphan" },
                    Members = []
                }
            ]
        };

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorFieldGroupUnreachable
            && e.Id == "orphan");
    }

    [Fact]
    public void Validate_WhenVariableInitialValueIsNull_ReturnsNullForbiddenError()
    {
        var contract = new FormContract
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
                    VariableId = VariableId.From("age"),
                    Name = "Age",
                    ValueType = ParseJsonElement("""{"kind":"integer"}"""),
                    InitialValue = ParseJsonElement("null")
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("ageField"),
                    VariableId = VariableId.From("age"),
                    Label = new LocalizedText { ["de"] = "Alter" },
                    Editability = FieldEditability.UserEditable,
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("ageField") }]
                }
            ]
        };

        var result = FormContractPreflight.Validate(contract);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorNullForbidden
            && e.Id == "age");
    }

    [Fact]
    public void Validate_WhenRuleExpressionReferencesUnknownVariable_ReturnsWarnExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""{"kind":"isSet","variableId":"nonExistentVar"}""");

        var contract = new FormContract
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
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
            ],
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "rule1",
                    Scope = "onInit",
                    When = whenExpression,
                    Then =
                    [
                        ParseJsonElement("""{"kind":"setVisible","fieldId":"nameField","visible":true}""")
                    ]
                }
            ]
        };

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    /// <summary>
    /// Prüft, dass unvollständige Übersetzungen bei <c>strictLocalization = false</c>
    /// die Warnung <c>W_LOCALIZATION_PARTIAL</c> auslösen.
    /// </summary>
    [Fact]
    public void Validate_WhenPartialLocalizationWithStrictOff_EmitsWarnLocalizationPartial()
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w => w.Code == PreflightCodes.WarnLocalizationPartial);
    }

    [Fact]
    public void Validate_WhenNotEqualsExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""
            {"kind":"notEquals","left":{"kind":"var","variableId":"nonExistentVar"},"right":{"kind":"const","value":"x"}}
            """);

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenCompareExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""
            {"kind":"compare","left":{"kind":"var","variableId":"nonExistentVar"},"right":{"kind":"const","value":10},"operator":"gt"}
            """);

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenInExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""
            {"kind":"in","value":{"kind":"var","variableId":"nonExistentVar"},"set":[{"kind":"const","value":"a"}]}
            """);

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenContainsExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""
            {"kind":"contains","text":{"kind":"var","variableId":"nonExistentVar"},"substring":{"kind":"const","value":"test"}}
            """);

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenStartsWithExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""
            {"kind":"startsWith","text":{"kind":"var","variableId":"nonExistentVar"},"prefix":{"kind":"const","value":"pre"}}
            """);

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenIsEmptyExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""{"kind":"isEmpty","variableId":"nonExistentVar"}""");

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenIsNotEmptyExpressionReferencesUnknownVariable_WarnsExprVarUnknown()
    {
        var whenExpression = ParseJsonElement("""{"kind":"isNotEmpty","variableId":"nonExistentVar"}""");

        var contract = CreateMinimalContract(whenExpression);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Warnings, w =>
            w.Code == PreflightCodes.WarnExprVarUnknown
            && w.Id == "nonExistentVar");
    }

    [Fact]
    public void Validate_WhenSpeakActionReferencesUnknownPrompt_ProducesError()
    {
        var contract = CreateMinimalContractWithAction(
            ParseJsonElement("""{"kind":"speak","promptId":"nonExistentPrompt"}"""));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "nonExistentPrompt");
    }

    [Fact]
    public void Validate_WhenCopyValueActionReferencesUnknownVariable_ProducesError()
    {
        var contract = CreateMinimalContractWithAction(
            ParseJsonElement("""{"kind":"copyValue","fromVariableId":"unknownFrom","toVariableId":"unknownTo"}"""));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "unknownFrom");
        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "unknownTo");
    }

    [Fact]
    public void Validate_WhenNavigateToFieldGroupActionReferencesUnknownGroup_ProducesError()
    {
        var contract = CreateMinimalContractWithAction(
            ParseJsonElement("""{"kind":"navigateToFieldGroup","fieldGroupId":"nonExistentGroup"}"""));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "nonExistentGroup");
    }

    [Fact]
    public void Validate_WhenFocusFieldActionReferencesUnknownField_ProducesError()
    {
        var contract = CreateMinimalContractWithAction(
            ParseJsonElement("""{"kind":"focusField","fieldId":"nonExistentField"}"""));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "nonExistentField");
    }

    [Fact]
    public void Validate_WhenClearValueActionReferencesUnknownVariable_ProducesError()
    {
        var contract = CreateMinimalContractWithAction(
            ParseJsonElement("""{"kind":"clearValue","variableId":"nonExistentVar"}"""));

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorRefMissing
            && e.Id == "nonExistentVar");
    }

    /// <summary>
    /// Erstellt einen Testvertrag mit dem übergebenen Bedingungsausdruck und einer
    /// <c>setVisible</c>-Aktion für das vorhandene Feld.
    /// </summary>
    private static FormContract CreateMinimalContract(JsonElement whenExpression)
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
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
            ],
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "rule1",
                    Scope = "onValueChanged",
                    When = whenExpression,
                    Then =
                    [
                        ParseJsonElement("""{"kind":"setVisible","target":{"kind":"field","fieldId":"nameField"},"visible":true}""")
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// Erstellt einen Testvertrag mit einer <c>isSet</c>-Bedingung für die vorhandene
    /// Variable und der übergebenen Aktion.
    /// </summary>
    private static FormContract CreateMinimalContractWithAction(JsonElement action)
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
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
            ],
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "rule1",
                    Scope = "onValueChanged",
                    When = ParseJsonElement("""{"kind":"isSet","variableId":"name"}"""),
                    Then = [action]
                }
            ]
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    [Fact]
    public void Validate_WhenNoInputTimeoutMsNegative_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithFormVoiceProfile(new FormVoiceProfile
        {
            NoInputTimeoutMs = -1
        });

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    [Fact]
    public void Validate_WhenSttConfidenceToAcceptExceeds100_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithFormVoiceProfile(new FormVoiceProfile
        {
            SttMinConfidenceToAccept = 101
        });

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    [Fact]
    public void Validate_WhenSttConfidenceInverted_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithFormVoiceProfile(new FormVoiceProfile
        {
            SttMinConfidenceToAccept = 50,
            SttMinConfidenceToConfirm = 90
        });

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.projections.voice.formVoiceProfile.sttMinConfidenceToConfirm");
    }

    [Fact]
    public void Validate_WhenVoiceHintsAllValid_NoSettingsError()
    {
        var contract = CreateMinimalContractWithFormVoiceProfile(new FormVoiceProfile
        {
            NoInputTimeoutMs = 5000,
            NoInputMaxRetries = 3,
            SttMinConfidenceToAccept = 85,
            SttMinConfidenceToConfirm = 60,
            PauseAfterPromptMs = 200,
            SpeechRate = 1.0
        });

        var result = FormContractPreflight.Validate(contract);

        Assert.DoesNotContain(result.Errors, e => e.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    private static FormContract CreateMinimalContractWithFormVoiceProfile(FormVoiceProfile formVoiceProfile)
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
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "Name",
                    ValueType = ParseJsonElement("""{"kind":"string"}""")
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
            ],
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection { FormVoiceProfile = formVoiceProfile }
            }
        };
    }

    private static FormContract CreateMinimalContractWithValueType(string valueTypeJson)
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
            StrictLocalization = false,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("v1"),
                    Name = "Var1",
                    ValueType = ParseJsonElement(valueTypeJson)
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("f1"),
                    VariableId = VariableId.From("v1"),
                    Label = new LocalizedText { ["de"] = "Feld" },
                    Editability = FieldEditability.UserEditable,
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("f1") }]
                }
            ]
        };
    }

    [Fact]
    public void Validate_WhenStringTypeMinLengthExceedsMaxLength_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"string","minLength":10,"maxLength":5}""");

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.variables[0].valueType");
    }

    [Fact]
    public void Validate_WhenSetTypeMinSelectedExceedsMaxSelected_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"set","values":[{"value":"a","label":{"de":"A"}},{"value":"b","label":{"de":"B"}}],"minSelected":3,"maxSelected":1}""");

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.variables[0].valueType");
    }

    [Fact]
    public void Validate_WhenDecimalTypeScaleExceedsPrecision_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"decimal","precision":5,"scale":8}""");

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.variables[0].valueType");
    }

    [Theory]
    [InlineData("""{"kind":"decimal","precision":5,"scale":2}""")]
    [InlineData("""{"kind":"dateTime"}""")]
    [InlineData("""{"kind":"object","properties":{}}""")]
    [InlineData("""{"kind":"list","itemType":{"kind":"string"}}""")]
    [InlineData("""{"kind":"custom","typeName":"domain-type"}""")]
    public void Validate_WhenValueTypeUsesPf09Extension_EmitsFeatureNotSupported(string valueTypeJson)
    {
        var contract = CreateMinimalContractWithValueType(valueTypeJson);

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorFeatureNotSupported
            && e.Path == "$.variables[0].valueType");
    }

    [Fact]
    public void Validate_WhenValueTypeRangesValid_NoSettingsError()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"string","minLength":1,"maxLength":100}""");

        var result = FormContractPreflight.Validate(contract);

        Assert.DoesNotContain(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.variables[0].valueType");
    }

    [Fact]
    public void Validate_WhenTopLevelPatternRegexInvalid_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"string","minLength":1,"maxLength":100}""");

        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "invalid-pattern",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("f1") },
                Severity = ConstraintSeverity.Error,
                Code = "E_PATTERN_INVALID",
                Message = new LocalizedText { ["de"] = "Ungueltiges Muster." },
                Kind = ConstraintKind.Pattern,
                AdditionalProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["pattern"] = ParseJsonElement("\"(\"")
                }
            }
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.constraints[0].pattern"
            && e.Id == "invalid-pattern");
    }

    [Fact]
    public void Validate_WhenVariableInvariantPatternRegexInvalid_EmitsSettingsInvalid()
    {
        var contract = CreateMinimalContractWithValueType(
            """{"kind":"string","minLength":1,"maxLength":100}""");

        contract.Variables[0].InvariantConstraints =
        [
            ParseJsonElement(
                """
                {
                  "constraintId": "invalid-variable-pattern",
                  "target": { "kind": "variable", "variableId": "v1" },
                  "severity": "error",
                  "code": "E_PATTERN_INVALID",
                  "message": { "de": "Ungueltiges Muster." },
                  "kind": "pattern",
                  "pattern": "("
                }
                """)
        ];

        var result = FormContractPreflight.Validate(contract);

        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid
            && e.Path == "$.variables[0].invariantConstraints[0].pattern"
            && e.Id == "invalid-variable-pattern");
    }
}
