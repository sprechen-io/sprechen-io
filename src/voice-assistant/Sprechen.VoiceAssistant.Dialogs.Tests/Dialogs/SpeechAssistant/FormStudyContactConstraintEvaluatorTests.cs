using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Datumsvergleich und Datumsbereich mit eigenständig aufgebauten
/// Beispielregeln: Wirksamkeitsdatum nach Geburtsdatum und festgelegte
/// Datumsgrenzen. Ein aktueller Studienvertrag wird nicht geladen.
/// </summary>
public sealed class FormStudyContactConstraintEvaluatorTests
{
    [Fact]
    public void CompareVariables_FlagsEffectiveDateBeforeBirthDate_AndPassesWhenOnOrAfter()
    {
        var values = new Dictionary<string, FormValue>(StringComparer.Ordinal)
        {
            ["birthDate"] = FormValue.FromString("1990-05-15"),
            ["changeEffectiveDate"] = FormValue.FromString("1980-01-01")
        };
        var evaluator = new FormCompareVariablesConstraintEvaluator(
            id => values.TryGetValue(id, out var value) ? value : (FormValue?)null);
        var constraint = CreateCompareVariablesConstraint("changeEffectiveDate", "birthDate", "gte");

        // Das frühere Wirksamkeitsdatum verletzt die Vergleichsregel.
        Assert.False(evaluator.IsSatisfied(constraint));

        // Das spätere Wirksamkeitsdatum erfüllt die Vergleichsregel.
        values["changeEffectiveDate"] = FormValue.FromString("2026-07-01");
        Assert.True(evaluator.IsSatisfied(constraint));
    }

    [Fact]
    public void DateRange_RejectsFutureBirthDate_AndAcceptsPlausiblePastDate()
    {
        var evaluator = new FormDateSetValueConstraintEvaluator();
        var constraint = CreateDateRangeConstraint("1900-01-01", "2025-12-31");

        // 2030 liegt nach der festen Obergrenze 2025-12-31.
        Assert.False(evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString("2030-01-01")));

        // 1990 liegt innerhalb des festgelegten Datumsbereichs.
        Assert.True(evaluator.IsSatisfied(ConstraintKinds.DateRange, constraint, FormValue.FromString("1990-05-15")));
    }

    private static ConstraintDefinition CreateCompareVariablesConstraint(
        string leftVariableId,
        string rightVariableId,
        string @operator)
        => new()
        {
            ConstraintId = "changeEffectiveDateOnOrAfterBirthDate",
            Kind = ConstraintKind.CompareVariables,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("changeEffectiveDateField") },
            Scope = ConstraintScope.CrossGroup,
            Severity = ConstraintSeverity.Error,
            Code = "form.dateOrder",
            Message = new LocalizedText { ["de"] = "Fehler" },
            AdditionalProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.LeftVariableId] = JsonSerializer.SerializeToElement(leftVariableId),
                [ConstraintPropertyNames.RightVariableId] = JsonSerializer.SerializeToElement(rightVariableId),
                [ConstraintPropertyNames.Operator] = JsonSerializer.SerializeToElement(@operator)
            }
        };

    private static ConstraintDefinition CreateDateRangeConstraint(string minDate, string maxDate)
        => new()
        {
            ConstraintId = "birthDatePlausibleRange",
            Kind = ConstraintKind.DateRange,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("birthDateField") },
            Severity = ConstraintSeverity.Error,
            Code = "form.birthDate.invalid",
            Message = new LocalizedText { ["de"] = "Fehler" },
            AdditionalProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.MinDate] = JsonSerializer.SerializeToElement(minDate),
                [ConstraintPropertyNames.MaxDate] = JsonSerializer.SerializeToElement(maxDate)
            }
        };
}
