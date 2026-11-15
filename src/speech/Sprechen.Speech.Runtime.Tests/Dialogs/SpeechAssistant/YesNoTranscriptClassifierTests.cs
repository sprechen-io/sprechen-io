namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class YesNoTranscriptClassifierTests
{
    private readonly YesNoTranscriptClassifier _classifier = new();
    private readonly IFormTranscriptParser _parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

    public static IEnumerable<object[]> SupportedLanguages
        => VoiceInterfaceModelCatalog.SupportedLanguages.Select(isoCode => new object[] { isoCode });

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void YesWord_ClassifiesAsYes_ForEverySupportedLanguage(string isoCode)
    {
        var yesWord = YesNo.GetYesWord(isoCode);

        Assert.Equal(YesNoClassification.Yes, _classifier.Classify(isoCode, yesWord));

        var outcome = _parser.ParseYesNo(isoCode, yesWord);

        Assert.True(outcome.Success);
        Assert.Equal("true", outcome.CanonicalValue);
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void NoWord_ClassifiesAsNo_ForEverySupportedLanguage(string isoCode)
    {
        var noWord = YesNo.GetNoWord(isoCode);

        Assert.Equal(YesNoClassification.No, _classifier.Classify(isoCode, noWord));

        var outcome = _parser.ParseYesNo(isoCode, noWord);

        Assert.True(outcome.Success);
        Assert.Equal("false", outcome.CanonicalValue);
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void NoEvidence_NeverClassifiesAsYes_ForEverySupportedLanguage(string isoCode)
    {
        foreach (var noToken in VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode))
        {
            Assert.NotEqual(YesNoClassification.Yes, _classifier.Classify(isoCode, noToken));
        }
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void EveryProfile_HasNonEmptyYesAndNoTokenSets(string isoCode)
    {
        Assert.NotEmpty(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode));
        Assert.NotEmpty(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode));
    }

    // Die modellierte Zustimmung unterscheidet bewusst zwischen den Sprachen:
    // Deutsch und Englisch akzeptieren ok und okay, Italienisch nur okay;
    // Französisch und Spanisch akzeptieren keines der beiden Wörter.
    // Eine Vereinheitlichung würde damit das angenommene Antwortvokabular ändern.
    // Der Befehlsanker wird unabhängig davon über die Aktivierungswörter bestimmt.
    [Theory]
    [InlineData("de", true, true)]
    [InlineData("en", true, true)]
    [InlineData("it", false, true)]
    [InlineData("fr", false, false)]
    [InlineData("es", false, false)]
    public void YesTokens_BareOkOkayMembership_FollowsRecordedPerLanguageAsymmetry(
        string isoCode,
        bool acceptsBareOk,
        bool acceptsBareOkay)
    {
        var yesTokens = VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode);

        Assert.Equal(acceptsBareOk, yesTokens.Contains("ok", StringComparer.OrdinalIgnoreCase));
        Assert.Equal(acceptsBareOkay, yesTokens.Contains("okay", StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void EveryMultiWordNoToken_IsMatchedAsNo(string isoCode)
    {
        var multiWordNoTokens = VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(isoCode)
            .Where(token => token.Contains(' ', StringComparison.Ordinal))
            .ToArray();

        foreach (var noToken in multiWordNoTokens)
        {
            Assert.Equal(YesNoClassification.No, _classifier.Classify(isoCode, noToken));
        }
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void NegationCuePrefixedToYesToken_NeverClassifiesAsYes(string isoCode)
    {
        var cues = VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(isoCode);
        if (cues.Count == 0)
        {
            return;
        }

        var firstCue = cues.First();
        var firstYesToken = VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(isoCode).First();

        Assert.NotEqual(YesNoClassification.Yes, _classifier.Classify(isoCode, $"{firstCue} {firstYesToken}"));
    }

    [Theory]
    [InlineData("nicht korrekt")]
    [InlineData("nicht ganz richtig")]
    [InlineData("nicht ok")]
    [InlineData("Das ist nicht korrekt")]
    public void GermanNegationCueWithYesWord_IsAmbiguous(string input)
    {
        Assert.Equal(YesNoClassification.Ambiguous, _classifier.Classify("de", input));
    }

    [Fact]
    public void GermanNegationCueAlone_ClassifiesAsNotRecognized()
    {
        Assert.Equal(YesNoClassification.NotRecognized, _classifier.Classify("de", "nicht"));
    }

    [Theory]
    [InlineData("keine Ahnung")]
    [InlineData("kein Problem")]
    public void GermanNegationCueIdiom_ClassifiesAsNotRecognized(string input)
    {
        Assert.Equal(YesNoClassification.NotRecognized, _classifier.Classify("de", input));
    }

    [Fact]
    public void MixedDirectYesAndNoTokens_StayAmbiguous()
    {
        Assert.Equal(YesNoClassification.Ambiguous, _classifier.Classify("de", "ja nein"));
    }
}
