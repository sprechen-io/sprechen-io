using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den ergänzenden Hinweis auf optionale Felder beim Fortschritt der Pflichtfelder.
/// Damit wird erklärt, warum die Gesamtzahl aller Eingabefelder größer sein kann.
/// Singular und Plural werden in fünf Sprachen verglichen; ohne optionale Felder
/// wird geprüft, dass kein zusätzlicher, durch Punkt und Leerzeichen getrennter Satz erscheint.
/// </summary>
public sealed class FormStatusOptionalFieldsHintTests
{
    [Theory]
    [InlineData("de", "Pflichtfelder abgeschlossen 0 von 6. Dazu 1 optionales Feld")]
    [InlineData("en", "required fields completed 0 of 6. Plus 1 optional field")]
    [InlineData("fr", "Champs obligatoires complétés 0 sur 6. Plus 1 champ facultatif")]
    [InlineData("es", "Campos obligatorios completados 0 de 6. Además 1 campo opcional")]
    [InlineData("it", "Campi obbligatori completati 0 su 6. Inoltre 1 campo facoltativo")]
    public void FormatProgressText_WithOneOptionalField_NamesItAfterTheRequiredProgress(
        string isoCode,
        string expected)
    {
        var status = CreateSnapshot(requiredCompleted: 0, requiredTotal: 6, allTotal: 7);

        var text = FormStatusPromptTextFormatter.FormatProgressText(
            contract: null,
            status,
            ProgressCalculationBasis.RequiredFieldsOnly,
            isoCode,
            PolitenessLevel.Formal);

        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData("de", "Pflichtfelder abgeschlossen 2 von 6. Dazu 3 optionale Felder")]
    [InlineData("en", "required fields completed 2 of 6. Plus 3 optional fields")]
    [InlineData("fr", "Champs obligatoires complétés 2 sur 6. Plus 3 champs facultatifs")]
    [InlineData("es", "Campos obligatorios completados 2 de 6. Además 3 campos opcionales")]
    [InlineData("it", "Campi obbligatori completati 2 su 6. Inoltre 3 campi facoltativi")]
    public void FormatProgressText_WithSeveralOptionalFields_UsesThePluralHint(
        string isoCode,
        string expected)
    {
        var status = CreateSnapshot(requiredCompleted: 2, requiredTotal: 6, allTotal: 9);

        var text = FormStatusPromptTextFormatter.FormatProgressText(
            contract: null,
            status,
            ProgressCalculationBasis.RequiredFieldsOnly,
            isoCode,
            PolitenessLevel.Formal);

        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void FormatProgressText_WithoutOptionalFields_StaysUnchanged(string isoCode)
    {
        var status = CreateSnapshot(requiredCompleted: 1, requiredTotal: 6, allTotal: 6);

        var text = FormStatusPromptTextFormatter.FormatProgressText(
            contract: null,
            status,
            ProgressCalculationBasis.RequiredFieldsOnly,
            isoCode,
            PolitenessLevel.Formal);

        Assert.DoesNotContain(". ", text, StringComparison.Ordinal);
    }

    private static FormStatusSnapshot CreateSnapshot(int requiredCompleted, int requiredTotal, int allTotal)
    {
        return new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "personal",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "street"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(requiredCompleted, allTotal),
                RequiredFieldsOnly: new FormStatusProgressDimension(requiredCompleted, requiredTotal),
                FieldGroupBased: new FormStatusProgressDimension(0, 2)),
            CurrentFieldLabel: "Straße",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Persönliche Daten",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);
    }
}
