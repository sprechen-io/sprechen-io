using System.Reflection;
using System.Text.Json;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

/// <summary>
/// Vergleicht Buchstabierbegriffe mit den direkt aus der eingebetteten Lexikonreferenz gelesenen Schlüsseln.
/// </summary>
public sealed class SpellingKeytermVocabularyDriftGuardTests
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Tests.Resources.form-transcript-lexicon.default.json";

    [Fact]
    public void DerivedKeyterms_EqualCanonicalSpellingTokenKeys()
    {
        var canonicalKeys = ReadCanonicalSpellingTokenKeys();
        var derived = SpellingKeytermVocabulary.Default.Keyterms;

        Assert.NotEmpty(canonicalKeys);
        Assert.Empty(canonicalKeys.Except(derived, StringComparer.Ordinal));
        Assert.Empty(derived.Except(canonicalKeys, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("mythe")]
    [InlineData("sac")]
    [InlineData("sacs")]
    [InlineData("cette")]
    public void Keyterms_DoNotBoostDigitsModeHomophones(string homophone)
    {
        // Diese französischen Wörter dürfen nicht im Buchstabier-Vokabular enthalten sein.
        var derived = SpellingKeytermVocabulary.Default.Keyterms;

        Assert.DoesNotContain(homophone, derived, StringComparer.Ordinal);
    }

    [Fact]
    public void DriftGuard_DetectsDivergence_WhenDerivedListMissesACanonicalKey()
    {
        var canonical = new[] { "alpha", "berta", "charlie" };
        var derived = new SpellingKeytermVocabulary(["alpha", "berta"]).Keyterms;

        Assert.NotEmpty(canonical.Except(derived, StringComparer.Ordinal));
    }

    [Fact]
    public void DriftGuard_DetectsDivergence_WhenDerivedListHasAnExtraEntry()
    {
        var canonical = new[] { "alpha", "berta" };
        var derived = new SpellingKeytermVocabulary(["alpha", "berta", "charlie"]).Keyterms;

        Assert.NotEmpty(derived.Except(canonical, StringComparer.Ordinal));
    }

    private static IReadOnlyList<string> ReadCanonicalSpellingTokenKeys()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(CanonicalResourceName)
            ?? throw new InvalidOperationException($"Canonical lexicon resource '{CanonicalResourceName}' is not embedded.");
        using var document = JsonDocument.Parse(stream);
        var spellingTokens = document.RootElement.GetProperty("spellingTokens");

        var keys = new List<string>();
        foreach (var token in spellingTokens.EnumerateObject())
        {
            keys.Add(token.Name);
        }

        return keys;
    }
}
