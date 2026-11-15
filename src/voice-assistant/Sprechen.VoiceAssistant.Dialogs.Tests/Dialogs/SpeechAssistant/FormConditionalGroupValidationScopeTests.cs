using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Pflichtangaben in bedingt sichtbaren Feldgruppen. Die Pflicht gilt nur,
/// während die Gruppe sichtbar ist, und entfällt beim erneuten Ausblenden.
/// So wird keine Eingabe in einem gerade nicht angebotenen Feld verlangt.
/// </summary>
public sealed class FormConditionalGroupValidationScopeTests
{
    [Fact]
    public async Task ValidateAll_WhenGateIsDeclined_IgnoresTheRequiredEntryInTheHiddenGroup()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateGatedContract(), "de", ImmutableDictionary<string, string>.Empty);

        var declined = await interpreter.TryApplyTranscriptAsync("nein");
        Assert.True(declined.ValueCaptured);

        // Die unveränderte Pflichtregel darf das Feld in der ausgeblendeten Gruppe nicht verlangen.
        Assert.Empty(await interpreter.ValidateAllAsync());
    }

    [Fact]
    public async Task ValidateAll_WhenGateIsAccepted_DemandsTheRequiredEntryInTheNowVisibleGroup()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateGatedContract(), "de", ImmutableDictionary<string, string>.Empty);

        var accepted = await interpreter.TryApplyTranscriptAsync("ja");
        Assert.True(accepted.ValueCaptured);

        var violation = Assert.Single(await interpreter.ValidateAllAsync());
        Assert.Equal("followUpRequired", violation.ConstraintId);
        Assert.Equal("followUpField", violation.FieldId);
    }

    [Fact]
    public async Task ValidateAll_WhenAnAcceptedGateIsWithdrawn_StopsDemandingTheEntryAgain()
    {
        // Auch nach vorherigem Einblenden muss das erneute Ausblenden die Pflichtprüfung aufheben.
        // Eine nur in eine Richtung wirkende Regel würde eine nicht mehr angebotene Eingabe verlangen.
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateGatedContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.True((await interpreter.TryApplyTranscriptAsync("ja")).ValueCaptured);
        Assert.NotEmpty(await interpreter.ValidateAllAsync());

        await interpreter.TryMoveFocusBackAsync();
        Assert.True((await interpreter.TryApplyTranscriptAsync("nein")).ValueCaptured);

        Assert.Empty(await interpreter.ValidateAllAsync());
    }

    // Das boolesche Feld steuert die Sichtbarkeit der Folgegruppe. Deren Pflichtregel hat keine
    // eigene Bedingung; die Begrenzung muss deshalb aus der Sichtbarkeit stammen.
    private static FormContract CreateGatedContract()
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
                    VariableId = VariableId.From("wantsFollowUp"),
                    Name = "wantsFollowUp",
                    ValueType = booleanValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("followUp"),
                    Name = "followUp",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("wantsFollowUpField"),
                    VariableId = VariableId.From("wantsFollowUp"),
                    Label = new LocalizedText { ["de"] = "Angaben machen?" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("followUpField"),
                    VariableId = VariableId.From("followUp"),
                    Label = new LocalizedText { ["de"] = "Angabe" },
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("wantsFollowUpField") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("followUp") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("followUp"),
                    Title = new LocalizedText { ["de"] = "Angaben" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("followUpField") }
                    ]
                }
            ],
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "toggleFollowUp",
                    Scope = "onValueChanged",
                    When = JsonDocument.Parse("{\"kind\":\"equals\",\"left\":{\"kind\":\"var\",\"variableId\":\"wantsFollowUp\"},\"right\":{\"kind\":\"const\",\"value\":true}}").RootElement,
                    Then =
                    [
                        JsonDocument.Parse("{\"kind\":\"setVisible\",\"target\":{\"kind\":\"fieldGroup\",\"fieldGroupId\":\"followUp\"},\"value\":true}").RootElement
                    ],
                    Else =
                    [
                        JsonDocument.Parse("{\"kind\":\"setVisible\",\"target\":{\"kind\":\"fieldGroup\",\"fieldGroupId\":\"followUp\"},\"value\":false}").RootElement
                    ]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "followUpRequired",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("followUpField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.required",
                    Message = new LocalizedText { ["de"] = "Die Angabe fehlt noch." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }
}
