using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormPromptCategoryBuilderTests
{
    #region GetUnrecognizedInputPrompt

    [Theory]
    [InlineData("de", "Entschuldigung")]
    [InlineData("en", "Sorry")]
    [InlineData("fr", "D\u00e9sol\u00e9")]
    public void GetUnrecognizedInputPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(isoCode);

        Assert.Contains(expectedContains, result);
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    #endregion

    #region GetEmptySummaryPrompt

    [Theory]
    [InlineData("de", "Noch keine Angaben")]
    [InlineData("en", "No answers yet")]
    [InlineData("es", "Todav\u00eda")]
    public void GetEmptySummaryPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormSummaryPromptBuilder.GetEmptySummaryPrompt(isoCode);

        Assert.Contains(expectedContains, result);
    }

    #endregion

    #region GetSummaryStoredValueLinePrompt

    [Theory]
    [InlineData("de", "Name ist Max")]
    [InlineData("en", "Name: Max")]
    [InlineData("fr", "Name : Max")]
    [InlineData("es", "Name: Max")]
    [InlineData("it", "Name: Max")]
    public void GetSummaryValueLinePrompt_ReturnsLocalizedText(string isoCode, string expected)
    {
        var result = FormSummaryPromptBuilder.GetSummaryValueLinePrompt(isoCode, "Name", "Max");

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("de", "Name ist gespeichert")]
    [InlineData("en", "Name: saved")]
    [InlineData("fr", "Name : enregistr\u00e9")]
    [InlineData("es", "Name: guardado")]
    [InlineData("it", "Name: salvato")]
    public void GetSummaryStoredValueLinePrompt_ReturnsLocalizedText(string isoCode, string expected)
    {
        var result = FormSummaryPromptBuilder.GetSummaryStoredValueLinePrompt(isoCode, "Name");

        Assert.Equal(expected, result);
    }

    #endregion

    #region GetNoErrorsPrompt

    [Theory]
    [InlineData("de", "keine Fehler")]
    [InlineData("en", "no errors")]
    public void GetNoErrorsPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormSummaryPromptBuilder.GetNoErrorsPrompt(isoCode);

        Assert.Contains(expectedContains, result);
    }

    #endregion

    #region GetJumpToMissingArgumentPrompt

    [Theory]
    [InlineData("de", "Namen des Feldes")]
    [InlineData("en", "name of the field")]
    public void GetJumpToMissingArgumentPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetJumpToMissingArgumentPrompt(isoCode, Sprechen.Mda.Contracts.V1.Catalog.PolitenessLevel.Formal);

        Assert.Contains(expectedContains, result);
    }

    #endregion

    #region GetJumpToNotFoundPrompt

    [Theory]
    [InlineData("de", "nicht gefunden")]
    [InlineData("en", "couldn't find")]
    public void GetJumpToNotFoundPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetJumpToNotFoundPrompt(isoCode);

        Assert.Contains(expectedContains, result);
    }

    #endregion

    #region GetCancelConfirmationPrompt

    [Theory]
    [InlineData("de", "abbrechen")]
    [InlineData("en", "cancel")]
    public void GetCancelConfirmationPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormConfirmationPromptBuilder.GetCancelConfirmationPrompt(isoCode, Sprechen.Mda.Contracts.V1.Catalog.PolitenessLevel.Formal);

        Assert.Contains(expectedContains, result);
        Assert.Contains("?", result);
    }

    [Fact]
    public void GetCancelConfirmationPrompt_InformalGerman_UsesDuAddressing()
    {
        var result = FormConfirmationPromptBuilder.GetCancelConfirmationPrompt("de", Sprechen.Mda.Contracts.V1.Catalog.PolitenessLevel.Informal);

        Assert.Contains("M\u00f6chtest du", result);
    }

    #endregion

    #region GetSingleFormOnlyPrompt

    [Theory]
    [InlineData("de", "nur ein Formular")]
    [InlineData("en", "only one form")]
    public void GetSingleFormOnlyPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetSingleFormOnlyPrompt(isoCode);

        Assert.Contains(expectedContains, result);
    }

    #endregion

    #region GetFormNotFoundPrompt

    [Theory]
    [InlineData("de", "nicht gefunden")]
    [InlineData("en", "couldn't find")]
    public void GetFormNotFoundPrompt_ReturnsLocalizedText(string isoCode, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetFormNotFoundPrompt(isoCode);

        Assert.Contains(expectedContains, result);
    }

    [Theory]
    [InlineData("fr", "formulaire")]
    [InlineData("es", "formulario")]
    [InlineData("it", "modulo")]
    public void GetSingleFormOnlyPrompt_SupportsAdditionalLanguages(string isoCode, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetSingleFormOnlyPrompt(isoCode);

        Assert.Contains(expectedContains, result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetAlreadyOnFormPrompt

    [Theory]
    [InlineData("de", "TestForm", "bereits")]
    [InlineData("en", "TestForm", "already")]
    public void GetAlreadyOnFormPrompt_ReturnsLocalizedTextWithFormName(string isoCode, string formName, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetAlreadyOnFormPrompt(isoCode, formName, Sprechen.Mda.Contracts.V1.Catalog.PolitenessLevel.Formal);

        Assert.Contains(expectedContains, result);
        Assert.Contains(formName, result);
    }

    #endregion

    #region GetSwitchFormConfirmationPrompt

    [Theory]
    [InlineData("de", "Kontaktformular", "Wechsle")]
    [InlineData("en", "Contact Form", "Switching")]
    public void GetSwitchFormConfirmationPrompt_ReturnsLocalizedTextWithFormName(string isoCode, string formName, string expectedContains)
    {
        var result = FormNavigationSwitchPromptBuilder.GetSwitchFormConfirmationPrompt(isoCode, formName);

        Assert.Contains(expectedContains, result);
        Assert.Contains(formName, result);
        Assert.Contains("?", result);
    }

    #endregion

    #region FormatOptionsPrompt

    [Theory]
    [InlineData("de", new string[0], "keine Optionen")]
    [InlineData("en", new string[0], "no options")]
    public void FormatOptionsPrompt_EmptyItems_ReturnsNoOptionsMessage(string isoCode, string[] items, string expectedContains)
    {
        var result = FormOptionsPromptBuilder.FormatOptionsPrompt(isoCode, items);

        Assert.Contains(expectedContains, result);
    }

    [Theory]
    [InlineData("de", new[] { "Rot", "Blau" }, "Zur Auswahl stehen")]
    [InlineData("en", new[] { "Red", "Blue" }, "Options are")]
    public void FormatOptionsPrompt_WithItems_ReturnsFormattedList(string isoCode, string[] items, string expectedContains)
    {
        var result = FormOptionsPromptBuilder.FormatOptionsPrompt(isoCode, items);

        Assert.Contains(expectedContains, result);
        foreach (var item in items)
        {
            Assert.Contains(item, result);
        }
    }

    #endregion

    #region GetInputModeChangedPrompt

    [Fact]
    public void GetInputModeChangedPrompt_ForSpelling_ReturnsSpellingFeedback()
    {
        var result = FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt("de", InputMode.Spelling);

        Assert.Contains("Buchstabiermodus", result);
    }

    [Fact]
    public void GetInputModeChangedPrompt_ForVoice_ReturnsVoiceFeedback()
    {
        var result = FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt("de", InputMode.Voice);

        Assert.Contains("Spracheingabe", result);
    }

    [Fact]
    public void GetInputModeChangedPrompt_ForKeyboard_ReturnsKeyboardFeedback()
    {
        var result = FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt("de", InputMode.Keyboard);

        Assert.Contains("Tastatureingabe", result);
    }

    [Fact]
    public void GetInputModeChangedPrompt_ForLiteral_ReturnsLiteralFeedback()
    {
        var result = FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt("de", InputMode.Literal);

        Assert.Contains("W\u00f6rtlichmodus", result);
    }

    #endregion

    #region SensitiveAndSafetyPrompts

    [Fact]
    public void GetLiteralModeCommandBlockedPrompt_ExplainsLiteralOff()
    {
        var result = FormInputFeedbackPromptBuilder.GetLiteralModeCommandBlockedPrompt("de");

        Assert.Contains("Befehle blockiert", result);
        Assert.Contains("w\u00f6rtlich aus", result);
    }

    [Fact]
    public void GetVoiceInputForbiddenPrompt_WithFallbackMentionsFallback()
    {
        var result = FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt("de", "die Tastatur");

        Assert.Contains("Spracheingabe deaktiviert", result);
        Assert.Contains("die Tastatur", result);
    }

    [Fact]
    public void GetReparableConfirmationPrompt_IncludesOptions()
    {
        var result = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            "de",
            "Name",
            "Max",
            includesValue: true);

        Assert.Contains("Name ist Max", result);
        Assert.Contains("\u00fcbernehmen", result);
        Assert.Contains("\u00e4ndern", result);
        Assert.Contains("buchstabieren", result);
    }

    [Fact]
    public void GetReparableSpellBackConfirmationPrompt_SpellsValueAndOffersAlternative()
    {
        var result = FormConfirmationPromptBuilder.GetReparableSpellBackConfirmationPrompt(
            "de",
            "Name",
            "Markus",
            "Martha, Anton, Richard, Kaufmann, Ulrich, Samuel",
            "Marcus");

        Assert.Contains("Name ist Markus", result);
        Assert.Contains("Martha, Anton, Richard, Kaufmann, Ulrich, Samuel", result);
        Assert.Contains("Marcus", result);
        Assert.Contains("buchstabieren", result);
    }

    [Theory]
    [InlineData("en", "Marcus")]
    [InlineData("fr", "Marcus")]
    [InlineData("es", "Marcus")]
    [InlineData("it", "Marcus")]
    public void GetReparableSpellBackConfirmationPrompt_SupportsAdditionalLanguages(string isoCode, string expectedAlternative)
    {
        var result = FormConfirmationPromptBuilder.GetReparableSpellBackConfirmationPrompt(
            isoCode,
            "Name",
            "Markus",
            "Martha, Anton, Richard, Kaufmann, Ulrich, Samuel",
            expectedAlternative);

        Assert.Contains("Martha, Anton, Richard, Kaufmann, Ulrich, Samuel", result);
        Assert.Contains(expectedAlternative, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetSilentInputOfferPrompt_WithFallbackMentionsFallback()
    {
        var result = FormInputFeedbackPromptBuilder.GetSilentInputOfferPrompt("de", "die Tastatur");

        Assert.Contains("sensible Angaben", result);
        Assert.Contains("die Tastatur", result);
    }

    [Fact]
    public void GetAmbiguousInputVsCommandPrompt_ContainsNumberedOptions()
    {
        var result = FormInputFeedbackPromptBuilder.GetAmbiguousInputVsCommandPrompt("de", "weiter bitte", "weiter");

        Assert.Contains("eins", result);
        Assert.Contains("zwei", result);
        Assert.Contains("weiter bitte", result);
        Assert.Contains("weiter", result);
    }

    [Fact]
    public void GetAmbiguousInputVsCommandPrompt_WithoutCommandLabel_UsesGenericCommandOption()
    {
        var result = FormInputFeedbackPromptBuilder.GetAmbiguousInputVsCommandPrompt("de", "weiter bitte", null);

        Assert.Contains("eins", result);
        Assert.Contains("zwei", result);
        Assert.Contains("als Befehl", result);
    }

    [Theory]
    [InlineData("fr", "accepter")]
    [InlineData("es", "aceptar")]
    [InlineData("it", "accettare")]
    public void GetReparableConfirmationPrompt_SupportsAdditionalLanguages(string isoCode, string expectedCommandWord)
    {
        var result = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            isoCode,
            "Name",
            "Max",
            includesValue: true);

        Assert.Contains(expectedCommandWord, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fr", SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard, "clavier")]
    [InlineData("es", SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard, "teclado")]
    [InlineData("it", SensitiveDataPolicyDefaults.SilentInputFallbackBraille, "braille")]
    public void GetSilentInputFallbackLabel_SupportsAdditionalLanguages(string isoCode, string fallback, string expectedPart)
    {
        var label = FormInputFeedbackPromptBuilder.GetSilentInputFallbackLabel(isoCode, fallback);

        Assert.NotNull(label);
        Assert.Contains(expectedPart, label!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetCancelRefractoryPrompt_ReturnsLocalizedText()
    {
        var result = FormConfirmationPromptBuilder.GetCancelRefractoryPrompt("de");

        Assert.Contains("gerade angefragt", result);
    }

    #endregion

    #region ModelFirstPrompts

    [Fact]
    public void GetUnrecognizedInputPrompt_WithContractPrompt_UsesModelPrompt()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.UnrecognizedInput,
            "Bitte aus Modell wiederholen.");

        var result = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
            contract,
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("Bitte aus Modell wiederholen.", result);
    }

    [Fact]
    public void GetSummaryStoredValueLinePrompt_WithTemplate_InterpolatesFieldLabel()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.SummaryStoredValue,
            "{{fieldLabel}}: aus Modell gespeichert.");

        var result = FormSummaryPromptBuilder.GetSummaryStoredValueLinePrompt(
            contract,
            "de",
            "Name",
            PolitenessLevel.Formal);

        Assert.Equal("Name: aus Modell gespeichert.", result);
    }

    [Fact]
    public void GetSummaryValueLinePrompt_WithTemplate_InterpolatesLabelAndValue()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.SummaryValueLine,
            "{{fieldLabel}} => {{fieldValue}}");

        var result = FormSummaryPromptBuilder.GetSummaryValueLinePrompt(
            contract,
            "de",
            "Name",
            "Max",
            PolitenessLevel.Formal);

        Assert.Equal("Name => Max", result);
    }

    [Fact]
    public void BuildSummaryPrompt_WithMixedItems_UsesPromptBuilderForAllSummaryLines()
    {
        var contract = CreatePromptContract(
            (FormDialogPromptIds.SummaryValueLine, "{{fieldLabel}} => {{fieldValue}}"),
            (FormDialogPromptIds.SummaryStoredValue, "{{fieldLabel}} => gespeichert"));

        var result = FormSummaryPromptBuilder.BuildSummaryPrompt(
            contract,
            "de",
            PolitenessLevel.Formal,
            [
                new FormSummaryItem("field1", "Name", "Max", FormSpokenValueProjectionStatus.FullValue),
                new FormSummaryItem("field2", "PIN", SpokenValue: null, FormSpokenValueProjectionStatus.Withheld)
            ]);

        Assert.Equal("Name => Max. PIN => gespeichert", result);
    }

    [Fact]
    public void GetReparableConfirmationPrompt_WithTemplate_InterpolatesConfirmationPrefix()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.ReparableConfirmation,
            "{{confirmationPrefix}}. Modell-Bestätigung.");

        var result = FormConfirmationPromptBuilder.GetReparableConfirmationPrompt(
            contract,
            "de",
            "Name",
            "Max",
            includesValue: true,
            PolitenessLevel.Formal);

        Assert.Equal("Name ist Max. Modell-Bestätigung.", result);
    }

    [Fact]
    public void FormatOptionsPrompt_WithTemplate_InterpolatesItems()
    {
        var contract = CreatePromptContract(
            FormDialogPromptIds.OptionsList,
            "Aus Modell: {{items}}.");

        var result = FormOptionsPromptBuilder.FormatOptionsPrompt(
            contract,
            "de",
            PolitenessLevel.Formal,
            ["eins", "zwei"]);

        Assert.Equal("Aus Modell: eins, zwei.", result);
    }

    #endregion

    #region FormatViolationPrompt

    [Fact]
    public void FormatViolationPrompt_WithRemediation_CombinesMessageAndRemediation()
    {
        var violation = new FormConstraintViolation(
            "field1",
            "constraint1",
            "error",
            "required",
            "Feld ist erforderlich.",
            "Bitte geben Sie einen Wert ein.",
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Contains("Feld ist erforderlich", result);
        Assert.Contains("Bitte geben Sie einen Wert ein", result);
    }

    [Fact]
    public void FormatViolationPrompt_WithoutRemediation_AppendsTheGenericRepairPrompt()
    {
        // Ohne modellierten Korrekturhinweis wird eine allgemeine Aufforderung zur erneuten
        // Eingabe ergänzt. Die Entscheidung hängt von der Textquelle ab, nicht von einer
        // inhaltlichen Bewertung der Meldung; weitere Fälle stehen in FormConstraintViolationPromptFormatterTests.
        var violation = new FormConstraintViolation(
            "field1",
            "constraint1",
            "error",
            "required",
            "Feld ist erforderlich.",
            null,
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal("Feld ist erforderlich. Bitte nennen Sie einen neuen Wert.", result);
    }

    [Fact]
    public void FormatViolationPrompt_WithWhitespaceRemediation_AppendsTheGenericRepairPrompt()
    {
        var violation = new FormConstraintViolation(
            "field1",
            "constraint1",
            "error",
            "required",
            "Feld ist erforderlich.",
            "   ",
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: null,
            understoodValue: null,
            includesValue: false);

        Assert.Equal("Feld ist erforderlich. Bitte nennen Sie einen neuen Wert.", result);
    }

    [Fact]
    public void FormatViolationPrompt_WithFieldLabelAndValue_PrefixesUnderstoodValueContextSentence()
    {
        var violation = new FormConstraintViolation(
            "birthDate",
            "c-date-future",
            "error",
            "future",
            "Das Datum muss in der Zukunft liegen.",
            "Bitte nennen Sie ein Datum in der Zukunft.",
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: "Geburtsdatum",
            understoodValue: "13. Mai 2026",
            includesValue: true);

        Assert.Equal(
            "Für Geburtsdatum habe ich 13. Mai 2026 verstanden. Das Datum muss in der Zukunft liegen. Bitte nennen Sie ein Datum in der Zukunft.",
            result);
    }

    [Fact]
    public void FormatViolationPrompt_WithMissingModeledMessage_SpeaksGenericFallbackAndNeverTheConstraintId()
    {
        var violation = new FormConstraintViolation(
            "iban",
            "c-iban-format",
            "error",
            "pattern",
            string.Empty,
            null,
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: "IBAN",
            understoodValue: null,
            includesValue: false);

        Assert.DoesNotContain("c-iban-format", result);
        Assert.Contains("Die Eingabe ist ungültig", result);
        Assert.Contains("IBAN", result);
    }

    [Fact]
    public void FormatViolationPrompt_WhenPrivacyForbidsValue_NamesFieldButNeverSpeaksTheValue()
    {
        var violation = new FormConstraintViolation(
            "password",
            "c-password-strength",
            "error",
            "pattern",
            "Das Passwort ist zu schwach.",
            null,
            ConstraintScopes.Field,
            null);

        var result = FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation,
            fieldLabel: "Passwort",
            understoodValue: "geheim123",
            includesValue: false);

        Assert.Contains("Passwort", result);
        Assert.DoesNotContain("geheim123", result);
    }

    [Fact]
    public void FormatViolationPrompt_NullViolation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FormConstraintViolationPromptFormatter.FormatViolationPrompt(
            CreatePromptContract(FormDialogPromptIds.OptionsNone, "Keine Optionen."),
            "de",
            PolitenessLevel.Formal,
            violation: null!,
            fieldLabel: null,
            understoodValue: null,
            includesValue: false));
    }

    #endregion

    private static FormContract CreatePromptContract(string promptId, string deText)
        => CreatePromptContract([(promptId, deText)]);

    private static FormContract CreatePromptContract(params (string PromptId, string DeText)[] prompts)
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts = [.. prompts.Select(prompt => new VoicePrompt
                        {
                            PromptId = prompt.PromptId,
                            Text = new LocalizedText
                            {
                                ["de"] = prompt.DeText
                            }
                        })]
                    }
                }
            }
        };
    }
}

