using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class SpellingKeytermVocabularyTests
{
    [Fact]
    public void Default_ReadsAlphabetWordsFromCanonicalLexicon()
    {
        var vocabulary = SpellingKeytermVocabulary.Default;

        Assert.NotEmpty(vocabulary.Keyterms);
        Assert.Contains("anton", vocabulary.Keyterms);
        Assert.Contains("alpha", vocabulary.Keyterms);
        Assert.Contains("eszett", vocabulary.Keyterms);
    }

    [Fact]
    public void Keyterms_AreDistinct()
    {
        var keyterms = SpellingKeytermVocabulary.Default.Keyterms;

        Assert.Equal(keyterms.Distinct(StringComparer.Ordinal).Count(), keyterms.Count);
    }

    [Fact]
    public void Constructor_DeduplicatesAndDropsEmptyWords()
    {
        var vocabulary = new SpellingKeytermVocabulary(["alpha", "alpha", "", "berta"]);

        Assert.Equal(["alpha", "berta"], vocabulary.Keyterms);
    }

    [Fact]
    public void Constructor_WhenEmptyInput_YieldsEmptyKeyterms()
    {
        var vocabulary = new SpellingKeytermVocabulary([]);

        Assert.Empty(vocabulary.Keyterms);
    }
}
