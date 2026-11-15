using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Prüft die Zuordnung von Parserfehlern zu Korrekturgründen. Für jeden Enumwert
// wird ein definierter Ergebniswert verlangt; separate Beispiele prüfen konkrete
// Zuordnungen. Ein bloßer gültiger Rückfallwert würde die erste Prüfung ebenfalls bestehen.
public sealed class FormRepairReasonResolverTests
{
    private static readonly FormRepairReasonResolver Resolver = new();

    private static FormRepairReasonResolver ResolverKnowing(params string[] known)
        => new(new FakeYesNoClassifier(known));

    [Fact]
    public void EveryParseFailureCategory_ResolvesToARepairReason()
    {
        var categories = Enum.GetValues<FormTranscriptParseFailureCategory>();

        Assert.NotEmpty(categories);
        foreach (var category in categories)
        {
            var reason = Resolver.Resolve(
                new FormTranscriptParseFailure(category),
                confidenceRejected: false,
                transcript: "irgendein transkript");

            Assert.True(
                Enum.IsDefined(reason),
                $"category {category} resolved to an undefined repair reason");
        }
    }

    [Theory]
    [InlineData(FormTranscriptParseFailureCategory.NotRecognized, FormRepairReason.UnparsedUtterance)]
    [InlineData(FormTranscriptParseFailureCategory.NoMatchingOption, FormRepairReason.NoMatchingOption)]
    [InlineData(FormTranscriptParseFailureCategory.AmbiguousChoice, FormRepairReason.AmbiguousChoice)]
    [InlineData(FormTranscriptParseFailureCategory.AmbiguousYesNo, FormRepairReason.AmbiguousYesNo)]
    [InlineData(FormTranscriptParseFailureCategory.UnknownSpellingToken, FormRepairReason.UnknownSpellingToken)]
    [InlineData(FormTranscriptParseFailureCategory.NumberOutOfRange, FormRepairReason.ValueOutOfRange)]
    [InlineData(FormTranscriptParseFailureCategory.InvalidCalendarDate, FormRepairReason.ValueOutOfRange)]
    [InlineData(FormTranscriptParseFailureCategory.TimeOutOfRange, FormRepairReason.ValueOutOfRange)]
    [InlineData(FormTranscriptParseFailureCategory.DecimalNotSupported, FormRepairReason.UnsupportedValueForm)]
    [InlineData(FormTranscriptParseFailureCategory.MultipleNumbers, FormRepairReason.UnsupportedValueForm)]
    [InlineData(FormTranscriptParseFailureCategory.UnsupportedTemporalForm, FormRepairReason.UnsupportedValueForm)]
    public void ParseFailureCategory_MapsToItsRepairReason(
        FormTranscriptParseFailureCategory category,
        FormRepairReason expected)
    {
        var reason = Resolver.Resolve(
            new FormTranscriptParseFailure(category),
            confidenceRejected: false,
            transcript: "blau");

        Assert.Equal(expected, reason);
    }

    // Eine wegen zu geringer Erkennungssicherheit abgelehnte Eingabe erhält diesen Korrekturgrund
    // unabhängig vom Parserergebnis und einer möglichen Wortschatzerkennung.
    [Fact]
    public void RejectedConfidence_WinsOverEveryOtherSignal()
    {
        var reason = Resolver.Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption),
            confidenceRejected: true,
            transcript: "ja");

        Assert.Equal(FormRepairReason.ConfidenceRejected, reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AbsentTranscript_ResolvesToNothingCaptured(string? transcript)
    {
        var reason = Resolver.Resolve(
            parseFailure: null,
            confidenceRejected: false,
            transcript: transcript);

        Assert.Equal(FormRepairReason.NothingCaptured, reason);
    }

    // Ein vorhandenes Transkript ohne Parserfehler erhält den allgemeinen Grund UnparsedUtterance.
    [Fact]
    public void TranscriptWithoutParseFailure_ResolvesToUnparsedUtterance()
    {
        var reason = Resolver.Resolve(
            parseFailure: null,
            confidenceRejected: false,
            transcript: "blau");

        Assert.Equal(FormRepairReason.UnparsedUtterance, reason);
    }

    // „Ja“ passt nicht zur Optionsliste, ist aber im Testklassifikator ein bekanntes
    // Zustimmungswort. Diese Kombination erhält einen eigenen Korrekturgrund.
    [Fact]
    public void KnownYesNoToken_OnAClosedOptionTurn_IsNotJustAnUnmatchedOption()
    {
        var reason = ResolverKnowing("Ja").Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption),
            confidenceRejected: false,
            transcript: "Ja",
            isoCode: "de");

        Assert.Equal(FormRepairReason.KnownVocabularyInvalidHere, reason);
    }

    [Fact]
    public void UnknownWord_OnAClosedOptionTurn_StaysNoMatchingOption()
    {
        var reason = ResolverKnowing("Ja").Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption),
            confidenceRejected: false,
            transcript: "Blaumeise",
            isoCode: "de");

        Assert.Equal(FormRepairReason.NoMatchingOption, reason);
    }

    // Die konkrete Mehrdeutigkeitsmeldung des Parsers hat Vorrang vor der Wortschatzerkennung.
    [Fact]
    public void SharperGrammarVerdicts_AreNotOverwrittenByTheVocabularyLookup()
    {
        var reason = ResolverKnowing("Ja").Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.AmbiguousYesNo),
            confidenceRejected: false,
            transcript: "Ja",
            isoCode: "de");

        Assert.Equal(FormRepairReason.AmbiguousYesNo, reason);
    }

    // Ohne Sprachangabe wird die zusätzliche Wortschatzklassifikation nicht angewendet.
    [Fact]
    public void WithoutALanguage_TheVocabularyClassIsNeverGuessed()
    {
        var reason = ResolverKnowing("Ja").Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption),
            confidenceRejected: false,
            transcript: "Ja",
            isoCode: null);

        Assert.Equal(FormRepairReason.NoMatchingOption, reason);
    }

    [Fact]
    public void RejectedConfidence_StillWins_EvenForKnownVocabulary()
    {
        var reason = ResolverKnowing("Ja").Resolve(
            new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption),
            confidenceRejected: true,
            transcript: "Ja",
            isoCode: "de");

        Assert.Equal(FormRepairReason.ConfidenceRejected, reason);
    }
}
