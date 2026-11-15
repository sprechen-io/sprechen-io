using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormValueParserOwnerTests
{
    [Fact]
    public void PreferredInputModeValueParser_WhenDigitsModeTargetsInteger_ReturnsCanonicalInteger()
    {
        var parser = new PreferredInputModeValueParser(new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()), DiacriticsStrategies.Preserve);

        var outcome = parser.Parse(
            "de",
            InputMode.Digits,
            EffectiveFieldInteractionTypes.Number,
            ValueTypeKinds.Integer,
            "1 2 3",
            out var handled);

        Assert.True(outcome.Success);
        Assert.True(handled);
        Assert.Equal("123", outcome.CanonicalValue);
    }

    [Fact]
    public void PreferredInputModeValueParser_WhenDigitsModeTargetsDate_DoesNotHandleRawBlob()
    {
        var parser = new PreferredInputModeValueParser(new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()), DiacriticsStrategies.Preserve);

        // Der Ziffernparser darf einen Datumswert nicht als rohe Ziffernfolge übernehmen.
        // Mit handled=false bleibt die Verarbeitung dem Datumsparser überlassen.
        var outcome = parser.Parse(
            "de",
            InputMode.Digits,
            EffectiveFieldInteractionTypes.Date,
            ValueTypeKinds.Date,
            "08011980",
            out var handled);

        Assert.False(handled);
        Assert.False(outcome.Success);
        Assert.NotEqual("08011980", outcome.CanonicalValue);
    }

    [Fact]
    public void PreferredInputModeValueParser_PassesTheSessionLanguageToTheDigitsPath()
    {
        var parser = new PreferredInputModeValueParser(new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()), DiacriticsStrategies.Preserve);

        var outcome = parser.Parse(
            "fr",
            InputMode.Digits,
            EffectiveFieldInteractionTypes.Number,
            ValueTypeKinds.Integer,
            "huit",
            out var handled);

        Assert.True(handled);
        Assert.True(outcome.Success);
        Assert.Equal("8", outcome.CanonicalValue);
    }

    [Fact]
    public void PreferredInputModeValueParser_WhenModeIsUnsupported_DoesNotHandle()
    {
        var parser = new PreferredInputModeValueParser(new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()), DiacriticsStrategies.Preserve);

        var outcome = parser.Parse(
            requestedIsoCode: "de",
            expectedInputMode: InputMode.Voice,
            EffectiveFieldInteractionTypes.Text,
            ValueTypeKinds.String,
            "text",
            out var handled);

        Assert.False(outcome.Success);
        Assert.False(handled);
    }

    [Fact]
    public void EffectiveInteractionValueParser_WhenBooleanInteraction_UsesRequestedLanguage()
    {
        var parser = new EffectiveInteractionValueParser("de", new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));

        var outcome = parser.Parse(
            EffectiveFieldInteractionTypes.Boolean,
            ValueTypeKinds.Boolean,
            "ja",
            ImmutableArray<FormChoiceOption>.Empty);

        Assert.True(outcome.Success);
        Assert.Equal("true", outcome.CanonicalValue);
    }

    [Fact]
    public void ParseInput_WhenBooleanTranscriptRejected_ReturnsFailureWithCategory()
    {
        var parser = new FormValueParser("de", new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));

        var outcome = parser.ParseInput(
            codeStyle: false,
            ValueTypeKinds.Boolean,
            "qwertz nonsense",
            ImmutableArray<FormChoiceOption>.Empty,
            preferredInputModes: null);

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Failure);
        Assert.Equal(FormTranscriptParseFailureCategory.NotRecognized, outcome.Failure!.Category);
    }

    [Fact]
    public void ParseFailure_NotRecognizedSingleton_HasEmptyCandidates()
    {
        Assert.Empty(FormTranscriptParseFailure.NotRecognized.Candidates);
    }

    [Fact]
    public void Outcome_Succeeded_WithNullValue_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => FormTranscriptParseOutcome.Succeeded(null!));
    }

    [Fact]
    public void RawFormValueParser_WhenSetValue_SplitsPipeSeparatedItems()
    {
        var result = RawFormValueParser.Default.TryParse("a| b |c", ValueTypeKinds.Set, out var value);

        Assert.True(result);
        Assert.Equal(["a", "b", "c"], value.Set.ToArray());
    }
}
