using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass eine verletzte Hinweisregel als Warnung gemeldet wird,
/// ohne im Status als blockierender Validierungsfehler zu zählen.
/// Die getrennte Behandlung von Meldung und Blockierung ist für einen Vergleich der Kanäle wichtig.
/// Dieser Test prüft die Sprachoberfläche, nicht die HTML-Oberfläche oder eine berechnete Fehlerquote.
/// </summary>
public sealed class FormAdvisoryConstraintReportingTests
{
    [Fact]
    public async Task ValidateAll_ReportsAnAdvisoryRuleWithTheSeverityItWasAuthoredWith()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateAdvisoryPatternContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.True((await interpreter.TryApplyTranscriptAsync("abc")).ValueCaptured);

        var violation = Assert.Single(await interpreter.ValidateAllAsync());
        Assert.Equal("phonePattern", violation.ConstraintId);
        Assert.Equal(SeverityLevels.Warning, violation.Severity);
    }

    [Fact]
    public async Task StatusSnapshot_DoesNotCountAnAdvisoryRuleAsAValidationError()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateAdvisoryPatternContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.True((await interpreter.TryApplyTranscriptAsync("abc")).ValueCaptured);

        // Die gemeldete Warnung zählt im Status nicht als blockierender Validierungsfehler.
        var status = await interpreter.GetStatusSnapshotAsync();
        Assert.Equal(0, status.ValidationErrorCount);
    }

    // Ein Feld mit einer Musterregel, deren Schweregrad Warning keine Eingabesperre auslösen soll.
    private static FormContract CreateAdvisoryPatternContract()
    {
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
                    VariableId = VariableId.From("phone"),
                    Name = "phone",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("phoneField"),
                    VariableId = VariableId.From("phone"),
                    Label = new LocalizedText { ["de"] = "Telefonnummer" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("phoneField") }
                    ]
                }
            ],
            Rules = [],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "phonePattern",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("phoneField") },
                    Severity = ConstraintSeverity.Warning,
                    Code = "form.phone.format",
                    Message = new LocalizedText { ["de"] = "Nur Ziffern." },
                    Kind = ConstraintKind.Pattern,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["pattern"] = JsonDocument.Parse("\"^[0-9]+$\"").RootElement
                    }
                }
            ]
        };
    }
}
