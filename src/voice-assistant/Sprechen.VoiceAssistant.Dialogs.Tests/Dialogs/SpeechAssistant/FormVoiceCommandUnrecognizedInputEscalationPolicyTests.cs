using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandUnrecognizedInputEscalationPolicyTests
{
    [Fact]
    public void SelectUnrecognizedInputPrompt_EscalatesAcrossAttempts()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "eins", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "zwei", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var third = policy.SelectUnrecognizedInputPrompt(context, field, "drei", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_FirstAttempt_WithTranscript_GroundsWhatWasUnderstood()
    {
        // Schon die erste Rückfrage enthält das vorhandene Transkript, damit die Rückmeldung die erfasste Eingabe benennt.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);
        var expected = FormInputFeedbackPromptBuilder.GetUnrecognizedInputWithTranscriptPrompt(
            context.Contract, context.IsoCode, "Hausnummer sieben", context.VoiceSettings.GetPolitenessLevel());

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "Hausnummer sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(expected, first);
        Assert.Contains("Hausnummer sieben", first, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_FirstAttempt_WithoutTranscript_SpeaksTheTranscriptFreeReAsk()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);
        var expected = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        var first = policy.SelectUnrecognizedInputPrompt(context, field, recognizedTranscript: null, FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(expected, first);
        Assert.DoesNotContain("\"\"", first, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_FirstAttempt_WithWhitespaceTranscript_SpeaksTheTranscriptFreeReAsk()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);
        var expected = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "   ", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(expected, first);
        Assert.DoesNotContain("\"\"", first, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_SecondAttempt_EchoesRecognizedTranscript()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);

        policy.SelectUnrecognizedInputPrompt(context, field, "egal", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "Hausnummer sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Hausnummer sieben", second, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_SecondAttempt_WithoutTranscript_DoesNotEchoEmptyQuotes()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);

        var first = policy.SelectUnrecognizedInputPrompt(context, field, recognizedTranscript: null, FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, recognizedTranscript: null, FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.DoesNotContain("\"\"", second, StringComparison.Ordinal);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Reset_RestartsTheLadderLikeASuccess()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String);

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "eins", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        policy.SelectUnrecognizedInputPrompt(context, field, "zwei", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        policy.Reset();
        var afterReset = policy.SelectUnrecognizedInputPrompt(context, field, "eins", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(first, afterReset);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_NewField_RestartsTheLadder()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var fieldA = CreateField(ValueTypeKinds.String, fieldId: "fieldA");
        var fieldB = CreateField(ValueTypeKinds.String, fieldId: "fieldB");

        var firstOnFieldA = policy.SelectUnrecognizedInputPrompt(context, fieldA, "eins", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        policy.SelectUnrecognizedInputPrompt(context, fieldA, "zwei", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var firstOnFieldB = policy.SelectUnrecognizedInputPrompt(context, fieldB, "eins", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(firstOnFieldA, firstOnFieldB);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void SecondFailure_OnDateField_SpeaksTheDateComponentFallbackOffer(string isoCode)
    {
        // Beim zweiten Fehlversuch am Datumsfeld enthält der Text das Angebot zur getrennten Eingabe der Datumsteile.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext(isoCode);
        var field = CreateField(ValueTypeKinds.Date);
        var expectedOffer = FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(
            context.Contract, isoCode, context.VoiceSettings.GetPolitenessLevel());

        policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains(expectedOffer, second, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void SecondFailure_OnFreeTextField_SpeaksTheSpellingFallbackOffer(string isoCode)
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext(isoCode);
        var field = CreateField(ValueTypeKinds.String);
        var expectedOffer = FormSpellingOverrideDiscoveryPromptBuilder.GetSpellingOverrideAvailablePrompt(
            context.Contract, isoCode, context.VoiceSettings.GetPolitenessLevel());

        policy.SelectUnrecognizedInputPrompt(context, field, "unverstaendlich", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "unverstaendlich", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains(expectedOffer, second, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void SecondFailure_OnNumberField_SpeaksTheDigitsFallbackOffer(string isoCode)
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext(isoCode);
        var field = CreateField(ValueTypeKinds.Integer);
        var expectedOffer = FormDigitsModeDiscoveryPromptBuilder.GetDigitsModeAvailablePrompt(
            context.Contract, isoCode, context.VoiceSettings.GetPolitenessLevel());

        policy.SelectUnrecognizedInputPrompt(context, field, "sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains(expectedOffer, second, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructiveFallbackOffer_FiresOnceAtSecondFailure_NotOnFirstOrThird()
    {
        // Unter den vier geprüften Rückfragen enthält nur die zweite das alternative Eingabeangebot.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Date);
        var expectedOffer = FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var third = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var fourth = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.DoesNotContain(expectedOffer, first, StringComparison.Ordinal);
        Assert.Contains(expectedOffer, second, StringComparison.Ordinal);
        Assert.DoesNotContain(expectedOffer, third, StringComparison.Ordinal);
        Assert.DoesNotContain(expectedOffer, fourth, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondFailure_OnDateField_WithoutPrimaryEntryFlag_KeepsThePlainLadder()
    {
        // Ohne Kennzeichnung als direkte Feldeingabe und ohne Freigabe des Alternativangebots bleibt die Rückfrage ohne Modusangebot.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Date);
        var expectedOffer = FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: false, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "achter Januar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: false, isAutoHelpDue: false);

        Assert.Contains("achter Januar", second, StringComparison.Ordinal);
        Assert.DoesNotContain(expectedOffer, second, StringComparison.Ordinal);
    }

    [Theory]
    // Für diese vier Typen verändert die Freigabe eines alternativen Modusangebots den zweiten Rückfragetext nicht.
    [InlineData(ValueTypeKinds.Time)]
    [InlineData(ValueTypeKinds.Enum)]
    [InlineData(ValueTypeKinds.Set)]
    [InlineData(ValueTypeKinds.Boolean)]
    public void SecondFailure_OnTypeWithoutModeSwitch_AppendsNoOffer(string valueTypeKind)
    {
        var withOffer = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var withoutOffer = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(valueTypeKind);

        withOffer.SelectUnrecognizedInputPrompt(context, field, "etwas", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var secondWithOffer = withOffer.SelectUnrecognizedInputPrompt(context, field, "etwas", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        withoutOffer.SelectUnrecognizedInputPrompt(context, field, "etwas", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var secondWithoutOffer = withoutOffer.SelectUnrecognizedInputPrompt(context, field, "etwas", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(secondWithoutOffer, secondWithOffer);
    }

    // Die Farboptionen sind in der Reihenfolge Rot, Grün, Blau angeordnet.
    private static readonly ImmutableArray<FormChoiceOption> ColorOptions = ImmutableArray.Create(
        new FormChoiceOption("red", "Rot"),
        new FormChoiceOption("green", "Grün"),
        new FormChoiceOption("blue", "Blau"));

    [Theory]
    // Bei einer nicht zuordenbaren Eingabe nennt die Rückfrage die Optionen und die Auswahl per Nummer in den fünf geprüften Sprachen.
    [InlineData("de", "Nummer")]
    [InlineData("en", "number")]
    [InlineData("fr", "numéro")]
    [InlineData("es", "número")]
    [InlineData("it", "numero")]
    public void ConstrainedField_FirstMiss_NamesOptionsAndNumber_WithoutEchoingTranscript(string isoCode, string numberWord)
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext(isoCode);
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);

        var prompt = policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Rot", prompt, StringComparison.Ordinal);
        Assert.Contains("Grün", prompt, StringComparison.Ordinal);
        Assert.Contains("Blau", prompt, StringComparison.Ordinal);
        Assert.Contains(numberWord, prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Klar", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstrainedField_SecondMiss_DoesNotEchoTranscript_NamesOptionsInstead()
    {
        // Auch beim zweiten Fehlversuch nennt das Auswahlfeld Optionen, ohne das nicht zugeordnete Transkript zu wiederholen.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);

        policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "Ja", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.DoesNotContain("Klar", second, StringComparison.Ordinal);
        Assert.DoesNotContain("Ja", second, StringComparison.Ordinal);
        Assert.Contains("Rot", second, StringComparison.Ordinal);
        Assert.Contains("Blau", second, StringComparison.Ordinal);
    }

    [Fact]
    public void MultiSelectField_Miss_AlsoSpeaksCooperativeOptionNaming()
    {
        // Für eine Mehrfachauswahl enthält die Rückfrage ebenfalls Optionen statt des nicht zugeordneten Transkripts.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Set, options: ColorOptions);

        var prompt = policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Rot", prompt, StringComparison.Ordinal);
        Assert.Contains("Blau", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Klar", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstrainedAndFreeTextFields_DivergeOnSecondMiss_OnlyFreeTextEchoes()
    {
        // Bei gleicher Rückfragestufe enthält Freitext das Transkript, die geschlossene Auswahl dagegen nicht.
        var context = CreateContext();

        var freeTextPolicy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var freeText = CreateField(ValueTypeKinds.String);
        freeTextPolicy.SelectUnrecognizedInputPrompt(context, freeText, "egal", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var freeTextSecond = freeTextPolicy.SelectUnrecognizedInputPrompt(context, freeText, "Hausnummer sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        var constrainedPolicy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var constrained = CreateField(ValueTypeKinds.Enum, options: ColorOptions);
        constrainedPolicy.SelectUnrecognizedInputPrompt(context, constrained, "egal", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var constrainedSecond = constrainedPolicy.SelectUnrecognizedInputPrompt(context, constrained, "Hausnummer sieben", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Hausnummer sieben", freeTextSecond, StringComparison.Ordinal);
        Assert.DoesNotContain("Hausnummer sieben", constrainedSecond, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstrainedField_ThirdMiss_StillEscalatesToTheHelpPointer()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);
        var expectedThird = FormInputFeedbackPromptBuilder.GetUnrecognizedInputThirdPrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        policy.SelectUnrecognizedInputPrompt(context, field, "Ja", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var third = policy.SelectUnrecognizedInputPrompt(context, field, "Nein", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: true, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(expectedThird, third);
    }

    [Fact]
    public void ConstrainedField_WithoutOptions_FallsBackToThePlainLadder()
    {
        // Ohne modellierte Optionen bleibt beim Auswahlfeld die Rückfrage mit Transkript erhalten.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum);

        policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "Klar", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Klar", second, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstrainedField_ReachedViaConfirmationReAsk_DoesNotGetTheCooperativeOptionPrompt()
    {
        // Außerhalb der direkten Werteingabe wird auch am Auswahlfeld die allgemeine Rückfrage ohne Optionsliste verwendet.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);
        var plainReAsk = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        var prompt = policy.SelectUnrecognizedInputPrompt(
            context, field, recognizedTranscript: null, FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: false, isAutoHelpDue: false);

        Assert.Equal(plainReAsk, prompt);
        Assert.DoesNotContain("Rot", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstrainedField_ReachedViaCommandRecognitionFailure_DoesNotGetTheCooperativeOptionPrompt()
    {
        // Für eine außerhalb der Werteingabe gemeldete Äußerung nennt die allgemeine Rückfrage das Transkript und keine Feldoptionen.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);
        var plainGroundedReAsk = FormInputFeedbackPromptBuilder.GetUnrecognizedInputWithTranscriptPrompt(
            context.Contract, context.IsoCode, "mach das Fenster zu", context.VoiceSettings.GetPolitenessLevel());

        var prompt = policy.SelectUnrecognizedInputPrompt(
            context, field, "mach das Fenster zu", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: false, isAutoHelpDue: false);

        Assert.Equal(plainGroundedReAsk, prompt);
        Assert.DoesNotContain("Rot", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectUnrecognizedInputPrompt_FieldWhoseValueMayNotBeSpoken_NeverQuotesTheTranscript()
    {
        // Wenn SpeakValueInConfirmation false ist, enthalten die beiden ersten Rückfragen das Transkript nicht.
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.String, speakValueInConfirmation: false);
        var transcriptFreeFirst = FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
            context.Contract, context.IsoCode, context.VoiceSettings.GetPolitenessLevel());

        var first = policy.SelectUnrecognizedInputPrompt(context, field, "DE89 3704 0044", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(context, field, "DE89 3704 0044", FormRepairReason.UnparsedUtterance, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Equal(transcriptFreeFirst, first);
        Assert.DoesNotContain("DE89", first, StringComparison.Ordinal);
        Assert.DoesNotContain("DE89", second, StringComparison.Ordinal);
    }

    private static FormFieldSnapshot CreateField(
        string valueTypeKind,
        bool codeStyle = false,
        string fieldId = "field",
        ImmutableArray<FormChoiceOption> options = default,
        bool speakValueInConfirmation = true)
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: fieldId, VariableId: fieldId, FieldGroupId: "group"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Group",
                Label: "Feld",
                HelpText: null,
                Examples: [],
                Options: options.IsDefault ? [] : options),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: codeStyle,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: valueTypeKind,
                SpeakValueInConfirmation: speakValueInConfirmation));

    private static FormVoiceCommandContext CreateContext(string isoCode = "de")
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { [isoCode] = "Form" },
            Description = new LocalizedText { [isoCode] = "Form" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { [isoCode] = "App" },
            Description = new LocalizedText { [isoCode] = "App" },
            SupportedLanguages = [isoCode],
            DefaultLanguage = isoCode,
            Forms = [form]
        };
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = [isoCode],
            EntryFieldGroupId = FieldGroupId.From("group"),
            StrictLocalization = true,
            Variables = [],
            Fields = [],
            FieldGroups = []
        };

        return new FormVoiceCommandContext(
            new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
            new FormVoiceCommandSelectionContext(
                new VoiceDialogSettings(),
                application,
                ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                form,
                contract),
            new FormVoiceCommandValueSnapshot(
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                []));
    }

    // Bekanntes, hier jedoch unzulässiges Vokabular wird in der Rückmeldung ausdrücklich benannt.
    [Fact]
    public void ConstrainedField_KnownVocabulary_NamesTheTokenInsteadOfRepeatingTheOptionList()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);

        var prompt = policy.SelectUnrecognizedInputPrompt(
            context, field, "Ja", FormRepairReason.KnownVocabularyInvalidHere,
            offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.Contains("Ja", prompt, StringComparison.Ordinal);
    }

    // Zwei unmittelbar aufeinanderfolgende Meldungen desselben bekannten Wortes erzeugen unterschiedliche Rückfragetexte.
    [Fact]
    public void ConstrainedField_SameKnownToken_IsNamedOnlyOncePerRun()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);

        var first = policy.SelectUnrecognizedInputPrompt(
            context, field, "Ja", FormRepairReason.KnownVocabularyInvalidHere,
            offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);
        var second = policy.SelectUnrecognizedInputPrompt(
            context, field, "Ja", FormRepairReason.KnownVocabularyInvalidHere,
            offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.NotEqual(first, second);
    }

    // Eine nicht passende Option wird nicht im Rückfragetext wiederholt.
    [Fact]
    public void ConstrainedField_MisheardWord_IsStillNeverQuoted()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions);

        var prompt = policy.SelectUnrecognizedInputPrompt(
            context, field, "Blaumeise", FormRepairReason.NoMatchingOption,
            offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.DoesNotContain("Blaumeise", prompt, StringComparison.Ordinal);
    }

    // Bei gesperrtem Rücklesen darf auch bekanntes Vokabular nicht als zitierter Eingabewert erscheinen.
    [Fact]
    public void SensitiveField_NeverNamesTheToken_EvenForKnownVocabulary()
    {
        var policy = new FormVoiceCommandUnrecognizedInputEscalationPolicy();
        var context = CreateContext();
        var field = CreateField(ValueTypeKinds.Enum, options: ColorOptions, speakValueInConfirmation: false);

        var prompt = policy.SelectUnrecognizedInputPrompt(
            context, field, "Ja", FormRepairReason.KnownVocabularyInvalidHere,
            offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false);

        Assert.DoesNotContain("\u201EJa\u201C", prompt, StringComparison.Ordinal);
    }
}
