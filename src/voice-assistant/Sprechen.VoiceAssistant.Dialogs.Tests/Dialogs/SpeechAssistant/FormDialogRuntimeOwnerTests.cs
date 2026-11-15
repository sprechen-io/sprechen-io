using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogRuntimeOwnerTests
{
    [Fact]
    public async Task TryMoveFocusNext_WhenSectionHiddenByRule_SkipsHiddenFields()
    {
        var contract = CreateVisibilityContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        Assert.Equal("shippingDifferentField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        var result = await interpreter.TryApplyTranscriptAsync("nein");

        Assert.True(result.ValueCaptured);
        Assert.Empty(result.Violations);

        Assert.False(await interpreter.TryMoveFocusNextAsync());
    }

    [Fact]
    public async Task TryMoveFocusNext_WhenSectionVisibleByRule_AllowsNavigatingIntoSection()
    {
        var contract = CreateVisibilityContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var result = await interpreter.TryApplyTranscriptAsync("ja");

        Assert.True(result.ValueCaptured);
        Assert.Empty(result.Violations);

        Assert.True(await interpreter.TryMoveFocusNextAsync());
        Assert.Equal("shippingStreetField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task ValidateAll_WhenRequiredMissing_ReturnsViolation()
    {
        var contract = CreateRequiredContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var violations = await interpreter.ValidateAllAsync();

        var violation = Assert.Single(violations);
        Assert.Equal("nameRequired", violation.ConstraintId);
        Assert.Equal("nameField", violation.FieldId);

        var result = await interpreter.TryApplyTranscriptAsync("Max");
        Assert.True(result.ValueCaptured);
        Assert.Empty(result.Violations);

        Assert.Empty(await interpreter.ValidateAllAsync());
    }

    [Fact]
    public async Task ValidateAll_WhenEndDateBeforeStartDate_ReturnsCrossFieldDateViolation()
    {
        // Die feldübergreifende Datumsregel meldet ihren Fehlercode, wenn das Enddatum vor dem Beginn liegt.
        // Der Test setzt die Werte direkt im Interpreter und durchläuft keinen Browserdialog.
        var contract = CreateDateOrderContract();
        var initialValues = ImmutableDictionary<string, string>.Empty
            .Add("startDate", "2026-04-10")
            .Add("endDate", "2026-04-01");
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        var violation = Assert.Single(await interpreter.ValidateAllAsync());
        Assert.Equal("endAfterStart", violation.ConstraintId);
        Assert.Equal("form.dateOrder", violation.Code);
        Assert.Equal("endDateField", violation.FieldId);
    }

    [Fact]
    public async Task ValidateAll_WhenEndDateAfterStartDate_ReturnsNoCrossFieldDateViolation()
    {
        var contract = CreateDateOrderContract();
        var initialValues = ImmutableDictionary<string, string>.Empty
            .Add("startDate", "2026-04-01")
            .Add("endDate", "2026-04-10");
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        Assert.Empty(await interpreter.ValidateAllAsync());
    }

    [Fact]
    public async Task ValidateAll_WhenFormTargetedCrossFieldFails_EmitsExactlyOneViolation()
    {
        // Eine Regel für das gesamte Formular darf nur einen Verstoß liefern,
        // damit sie nicht für jedes sichtbare Feld erneut gezählt wird.
        var contract = CreateFormTargetedCrossFieldContract();
        var initialValues = ImmutableDictionary<string, string>.Empty
            .Add("startDate", "2026-04-10")
            .Add("endDate", "2026-04-01");
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        var violation = Assert.Single(await interpreter.ValidateAllAsync());
        Assert.Equal("endAfterStart", violation.ConstraintId);
    }

    [Fact]
    public async Task TryApplyTranscript_WhenSingleSelect_SetsCanonicalCode()
    {
        var contract = CreateSingleSelectContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var result = await interpreter.TryApplyTranscriptAsync("Deutschland");

        Assert.True(result.ValueCaptured);

        var values = (await interpreter.GetValuesSnapshotAsync());
        Assert.Equal("DE", values["country"]);
    }

    [Fact]
    public async Task GetStatusSnapshot_TracksCompletionAcrossFieldsAndFieldGroups()
    {
        var contract = CreateTwoSectionRequiredContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var initial = (await interpreter.GetStatusSnapshotAsync());

        Assert.Equal(0, initial.Progress.AllFields.Completed);
        Assert.Equal(0, initial.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(0, initial.Progress.FieldGroupBased.Completed);
        Assert.Equal(2, initial.OpenRequiredFieldCount);
        Assert.Equal(2, initial.ValidationErrorCount);

        var firstApply = await interpreter.TryApplyTranscriptAsync("Max");
        Assert.True(firstApply.ValueCaptured);
        Assert.True(await interpreter.TryMoveFocusNextAsync());

        var afterFirstField = (await interpreter.GetStatusSnapshotAsync());
        Assert.Equal(1, afterFirstField.Progress.AllFields.Completed);
        Assert.Equal(1, afterFirstField.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(1, afterFirstField.Progress.FieldGroupBased.Completed);
        Assert.Equal(1, afterFirstField.OpenRequiredFieldCount);
        Assert.Equal(1, afterFirstField.ValidationErrorCount);
        Assert.Equal(2, afterFirstField.Navigation.CurrentFieldGroupIndex);
        Assert.Equal(2, afterFirstField.Navigation.TotalFieldGroups);

        var secondApply = await interpreter.TryApplyTranscriptAsync("Berlin");
        Assert.True(secondApply.ValueCaptured);

        var completed = (await interpreter.GetStatusSnapshotAsync());
        Assert.Equal(2, completed.Progress.AllFields.Completed);
        Assert.Equal(2, completed.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(2, completed.Progress.FieldGroupBased.Completed);
        Assert.Equal(0, completed.OpenRequiredFieldCount);
        Assert.Equal(0, completed.ValidationErrorCount);
    }

    [Fact]
    public async Task GetStatusSnapshot_WhenCrossGroupConstraintFails_KeepsUnrelatedFieldGroupsComplete()
    {
        var contract = CreateTwoSectionCrossGroupContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var firstApply = await interpreter.TryApplyTranscriptAsync("Max");
        Assert.True(firstApply.ValueCaptured);
        Assert.True(await interpreter.TryMoveFocusNextAsync());

        var secondApply = await interpreter.TryApplyTranscriptAsync("Berlin");
        Assert.True(secondApply.ValueCaptured);

        var status = (await interpreter.GetStatusSnapshotAsync());

        Assert.Equal(1, status.ValidationErrorCount);
        Assert.Equal(1, status.Progress.AllFields.Completed);
        Assert.Equal(1, status.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(1, status.Progress.FieldGroupBased.Completed);
        Assert.Equal(2, status.Navigation.TotalFieldGroups);
    }

    [Fact]
    public async Task GetStatusSnapshot_WhenOnlyNonRequiredErrorExists_DoesNotIncreaseOpenRequiredCount()
    {
        var contract = CreatePatternOnlyContract();
        var initialValues = ImmutableDictionary.Create<string, string>()
            .Add("postalCode", "abc");
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        var status = (await interpreter.GetStatusSnapshotAsync());

        Assert.Equal(1, status.ValidationErrorCount);
        Assert.Equal(0, status.OpenRequiredFieldCount);
    }

    [Fact]
    public async Task BuildSummaryItems_WhenReadbackIsDisabled_ReturnsStoredItemWithoutSpokenValue()
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
        };

        var initialValues = ImmutableDictionary.Create<string, string>()
            .Add("name", "Max");
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        var items = (await interpreter.BuildSummaryItemsAsync());

        var item = Assert.Single(items);
        Assert.Equal("nameField", item.FieldId);
        Assert.Equal("Name", item.FieldLabel);
        Assert.Null(item.SpokenValue);
        Assert.Equal(FormSpokenValueProjectionStatus.Withheld, item.Status);
    }

    [Fact]
    public async Task TrySetFocusedFieldInputMode_WhenSpellingIsSet_OverridesFieldInputMode()
    {
        var contract = CreateRequiredContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        Assert.Equal(InputMode.Voice, (await interpreter.GetFocusedFieldAsync()).InputMode);

        Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Spelling));

        var field = await interpreter.GetFocusedFieldAsync();
        Assert.Equal(InputMode.Spelling, field.InputMode);
    }

    [Fact]
    public async Task TrySetFocusedFieldInputMode_WhenModeUnsupportedForFieldType_ReturnsFalseAndPreservesPreviousOverride()
    {
        // Für ein boolesches Feld wird Buchstabieren abgelehnt; die Tastatureingabe bleibt eingestellt.
        var contract = CreateRequiredContract();
        contract.Variables[0].ValueType = JsonDocument.Parse("""{"kind":"boolean"}""").RootElement;
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Keyboard));

        var result = await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Spelling);

        Assert.False(result);
        Assert.Equal(InputMode.Keyboard, (await interpreter.GetFocusedFieldAsync()).InputMode);
    }

    [Fact]
    public async Task TrySetFocusedFieldInputMode_WhenFieldIsSecret_ReturnsFalseForMicrophoneModes()
    {
        // Für die Geheimhaltungsstufe Secret muss der Wechsel zum Buchstabieren abgelehnt werden.
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var result = await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Spelling);

        Assert.False(result);
        Assert.Equal(InputMode.Voice, (await interpreter.GetFocusedFieldAsync()).InputMode);
    }

    [Fact]
    public async Task TrySetFocusedFieldInputMode_WhenFieldNotEditable_ReturnsFalse()
    {
        // Ein zur Laufzeit schreibgeschütztes Feld darf nicht zum Buchstabiermodus wechseln.
        var contract = CreateRequiredContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        interpreter.MutableState.SetFieldReadOnly(FieldId.From("nameField"), readOnly: true);

        var result = await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Spelling);

        Assert.False(result);
        Assert.Equal(InputMode.Voice, (await interpreter.GetFocusedFieldAsync()).InputMode);
    }

    [Fact]
    public async Task IsVoiceInputAllowed_WhenPolicyForbidsVoice_ReturnsFalseAndFallback()
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
            SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
        };

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var result = await interpreter.IsVoiceInputAllowedAsync("name");

        Assert.False(result.Allowed);
        Assert.Equal(SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard, result.SilentInputFallback);
    }

    [Fact]
    public async Task RequiresReadbackOptIn_WhenPrivacyClassSensitive_ReturnsTrue()
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        Assert.True(await interpreter.RequiresReadbackOptInAsync("name"));
        Assert.False(await interpreter.CanSpeakValueInConfirmationAsync("name", explicitOptIn: false));
        Assert.True(await interpreter.CanSpeakValueInConfirmationAsync("name", explicitOptIn: true));
    }

    [Fact]
    public async Task IsSpellOutAllowedInConfirmation_WhenPrivacyClassSensitive_ReturnsFalse()
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        Assert.False(await interpreter.IsSpellOutAllowedInConfirmationAsync("name"));
    }

    [Theory]
    [InlineData("de", "Endet auf CDEF.")]
    [InlineData("en", "Ends with CDEF.")]
    [InlineData("fr", "Se termine par CDEF.")]
    [InlineData("es", "Termina en CDEF.")]
    [InlineData("it", "Termina con CDEF.")]
    public async Task BuildMaskedReadback_WhenPrivacyClassSensitive_ReturnsLocalizedSuffixPrompt(string isoCode, string expected)
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;

        var interpreter = new FormInterpreterRuntimeOwner(contract, isoCode, ImmutableDictionary<string, string>.Empty);

        var masked = await interpreter.BuildMaskedReadbackAsync("name", "ABCDEF", isoCode);

        Assert.Equal(expected, masked);
    }

    [Theory]
    [InlineData("de", "L\u00e4nge erfasst: 6 Zeichen.")]
    [InlineData("en", "Length captured: 6 characters.")]
    [InlineData("fr", "Longueur enregistr\u00e9e : 6 caract\u00e8res.")]
    [InlineData("es", "Longitud registrada: 6 caracteres.")]
    [InlineData("it", "Lunghezza registrata: 6 caratteri.")]
    public async Task BuildMaskedReadback_WhenPrivacyClassSecret_ReturnsLocalizedLengthPrompt(string isoCode, string expected)
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Secret;

        var interpreter = new FormInterpreterRuntimeOwner(contract, isoCode, ImmutableDictionary<string, string>.Empty);

        var masked = await interpreter.BuildMaskedReadbackAsync("name", "ABCDEF", isoCode);

        Assert.Equal(expected, masked);
    }

    [Fact]
    public async Task ShouldOfferSilentInputOption_WhenPrivacyClassSensitive_ReturnsTrue()
    {
        var contract = CreateRequiredContract();
        contract.Variables[0].PrivacyClass = PrivacyClass.Sensitive;

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var offer = await interpreter.ShouldOfferSilentInputOptionAsync("name");

        Assert.True(offer.Allowed);
        Assert.Equal(SensitiveDataPolicyDefaults.SilentInputFallbackAny, offer.SilentInputFallback);
    }

    private static FormContract CreateVisibilityContract()
    {
        var booleanValueType = JsonDocument.Parse("{\"kind\":\"boolean\"}").RootElement;
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("test"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("shippingDifferent"),
                    Name = "shippingDifferent",
                    ValueType = booleanValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("shippingStreet"),
                    Name = "shippingStreet",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("shippingDifferentField"),
                    VariableId = VariableId.From("shippingDifferent"),
                    Label = new LocalizedText { ["de"] = "Lieferadresse anders?" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("shippingStreetField"),
                    VariableId = VariableId.From("shippingStreet"),
                    Label = new LocalizedText { ["de"] = "Stra\u00dfe" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Root" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("shippingDifferentField") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("shipping") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("shipping"),
                    Title = new LocalizedText { ["de"] = "Lieferung" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("shippingStreetField") }
                    ]
                }
            ],
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "toggleShippingSection",
                    Scope = "onValueChanged",
                    When = JsonDocument.Parse("{\"kind\":\"equals\",\"left\":{\"kind\":\"var\",\"variableId\":\"shippingDifferent\"},\"right\":{\"kind\":\"const\",\"value\":false}}").RootElement,
                    Then =
                    [
                        JsonDocument.Parse("{\"kind\":\"setVisible\",\"target\":{\"kind\":\"fieldGroup\",\"fieldGroupId\":\"shipping\"},\"value\":false}").RootElement
                    ],
                    Else =
                    [
                        JsonDocument.Parse("{\"kind\":\"setVisible\",\"target\":{\"kind\":\"fieldGroup\",\"fieldGroupId\":\"shipping\"},\"value\":true}").RootElement
                    ]
                }
            ]
        };
    }

    private static FormContract CreateRequiredContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("required"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
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
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "nameRequired",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("nameField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Dieses Feld ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }

    private static FormContract CreateDateOrderContract()
    {
        var dateValueType = JsonDocument.Parse("{\"kind\":\"date\"}").RootElement;
        var comparison = JsonDocument.Parse(
            "{\"leftVariableId\":\"endDate\",\"rightVariableId\":\"startDate\",\"operator\":\"gte\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("date-order"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition { VariableId = VariableId.From("startDate"), Name = "startDate", ValueType = dateValueType },
                new VariableDefinition { VariableId = VariableId.From("endDate"), Name = "endDate", ValueType = dateValueType }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("startDateField"),
                    VariableId = VariableId.From("startDate"),
                    Label = new LocalizedText { ["de"] = "Startdatum" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("endDateField"),
                    VariableId = VariableId.From("endDate"),
                    Label = new LocalizedText { ["de"] = "Enddatum" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Zeitraum" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("startDateField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("endDateField") }
                    ]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "endAfterStart",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("endDateField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.dateOrder",
                    Message = new LocalizedText { ["de"] = "Das Enddatum muss nach dem Startdatum liegen." },
                    Remediation = new LocalizedText { ["de"] = "Bitte ein Enddatum am oder nach dem Startdatum angeben." },
                    Kind = ConstraintKind.CompareVariables,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["leftVariableId"] = comparison.GetProperty("leftVariableId"),
                        ["rightVariableId"] = comparison.GetProperty("rightVariableId"),
                        ["operator"] = comparison.GetProperty("operator")
                    }
                }
            ]
        };
    }

    private static FormContract CreateFormTargetedCrossFieldContract()
    {
        var contract = CreateDateOrderContract();
        contract.FormId = FormId.From("form-targeted-cross-field");

        contract.Constraints[0].Target = new ConstraintTargetRef { Kind = "form" };
        return contract;
    }

    private static FormContract CreateSingleSelectContract()
    {
        var valueTypeJson = """
            {
              "kind": "enum",
              "values": [
                { "code": "DE", "displayName": { "de": "Deutschland" } },
                { "code": "US", "displayName": { "de": "Vereinigte Staaten" } }
              ]
            }
            """;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("single-choice"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("country"),
                    Name = "country",
                    ValueType = JsonDocument.Parse(valueTypeJson).RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("countryField"),
                    VariableId = VariableId.From("country"),
                    Label = new LocalizedText { ["de"] = "Land" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("countryField") }]
                }
            ]
        };
    }

    private static FormContract CreateTwoSectionRequiredContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("status"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("personal"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("firstName"),
                    Name = "firstName",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("city"),
                    Name = "city",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("firstNameField"),
                    VariableId = VariableId.From("firstName"),
                    Label = new LocalizedText { ["de"] = "Vorname" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("cityField"),
                    VariableId = VariableId.From("city"),
                    Label = new LocalizedText { ["de"] = "Stadt" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("personal"),
                    Title = new LocalizedText { ["de"] = "Persönlich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("firstNameField") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("contact") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("contact"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("cityField") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "firstNameRequired",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("firstNameField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Vorname ist erforderlich." },
                    Kind = ConstraintKind.Required
                },
                new ConstraintDefinition
                {
                    ConstraintId = "cityRequired",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("cityField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Stadt ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }

    private static FormContract CreateTwoSectionCrossGroupContract()
    {
        var contract = CreateTwoSectionRequiredContract();
        contract.Constraints.Add(new ConstraintDefinition
        {
            ConstraintId = "crossGroupGuard",
            Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("firstNameField") },
            Scope = ConstraintScope.CrossGroup,
            Severity = ConstraintSeverity.Error,
            Code = "form.crossGroup.invalid",
            Message = new LocalizedText { ["de"] = "Ungueltige feldgruppenuebergreifende Kombination." },
            Kind = ConstraintKind.Pattern,
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                ["pattern"] = JsonDocument.Parse("\"^X$\"").RootElement
            }
        });

        return contract;
    }

    private static FormContract CreatePatternOnlyContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("pattern-only"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("postalCode"),
                    Name = "postalCode",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("postalCodeField"),
                    VariableId = VariableId.From("postalCode"),
                    Label = new LocalizedText { ["de"] = "PLZ" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("postalCodeField") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "postalCodePattern",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("postalCodeField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.pattern.invalid",
                    Message = new LocalizedText { ["de"] = "PLZ ungültig." },
                    Kind = ConstraintKind.Pattern,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["pattern"] = JsonDocument.Parse("\"^[0-9]{5}$\"").RootElement
                    }
                }
            ]
        };
    }

    [Fact]
    public async Task TryMoveFocusNext_RecordsFocusMovedEventUnderSessionId()
    {
        var interpreter = CreateNavigationInterpreter("session-next");

        Assert.Equal("field1", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.True(await interpreter.TryMoveFocusNextAsync());
        Assert.Equal("field2", (await interpreter.GetFocusedFieldAsync()).FieldId);

        var eventStore = Assert.IsType<InMemoryEventStore>(interpreter.EventStore);
        var events = await eventStore.GetEventsAsync("session-next");

        var focusEvent = Assert.IsType<FocusMovedEvent>(Assert.Single(events));
        Assert.Equal("field1", focusEvent.PreviousFieldId);
        Assert.Equal("field2", focusEvent.NewFieldId);
        Assert.Equal(FocusMovedReason.Next, focusEvent.Reason);
    }

    [Fact]
    public async Task TryMoveFocusBack_RecordsFocusMovedEventUnderSessionId()
    {
        var interpreter = CreateNavigationInterpreter("session-back");

        Assert.True(await interpreter.TryMoveFocusNextAsync());
        Assert.True(await interpreter.TryMoveFocusBackAsync());
        Assert.Equal("field1", (await interpreter.GetFocusedFieldAsync()).FieldId);

        var eventStore = Assert.IsType<InMemoryEventStore>(interpreter.EventStore);
        var events = await eventStore.GetEventsAsync("session-back");

        Assert.Equal(2, events.Length);
        var backEvent = Assert.IsType<FocusMovedEvent>(events[1]);
        Assert.Equal("field2", backEvent.PreviousFieldId);
        Assert.Equal("field1", backEvent.NewFieldId);
        Assert.Equal(FocusMovedReason.Back, backEvent.Reason);
    }

    [Fact]
    public async Task TryNavigateToFieldGroup_RecordsFieldGroupNavigatedEventUnderSessionId()
    {
        var interpreter = CreateNavigationInterpreter("session-field-group");

        Assert.True(await interpreter.TryNavigateToFieldGroupAsync("section2"));
        Assert.Equal("field2", (await interpreter.GetFocusedFieldAsync()).FieldId);

        var eventStore = Assert.IsType<InMemoryEventStore>(interpreter.EventStore);
        var events = await eventStore.GetEventsAsync("session-field-group");

        var fieldGroupEvent = Assert.IsType<FieldGroupNavigatedEvent>(Assert.Single(events));
        Assert.Equal("root", fieldGroupEvent.PreviousFieldGroupId);
        Assert.Equal("section2", fieldGroupEvent.NewFieldGroupId);
    }

    private static FormInterpreterRuntimeOwner CreateNavigationInterpreter(string sessionId)
    {
        return new FormInterpreterRuntimeOwner(
            CreateNavigationContract(),
            "de",
            ImmutableDictionary<string, string>.Empty,
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            PolitenessLevel.Formal,
            sessionId);
    }

    private static FormContract CreateNavigationContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("value1"),
                    Name = "value1",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("value2"),
                    Name = "value2",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("value3"),
                    Name = "value3",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("field1"),
                    VariableId = VariableId.From("value1"),
                    Label = new LocalizedText { ["de"] = "Erstes Feld" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("field2"),
                    VariableId = VariableId.From("value2"),
                    Label = new LocalizedText { ["de"] = "Zweites Feld" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("field3"),
                    VariableId = VariableId.From("value3"),
                    Label = new LocalizedText { ["de"] = "Drittes Feld" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Root" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("field1") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("section2") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("section2"),
                    Title = new LocalizedText { ["de"] = "Unterbereich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("field2") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("field3") }
                    ]
                }
            ]
        };
    }
}
