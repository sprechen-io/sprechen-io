using System.Reflection;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht Umlauttabellen und ASCII-Ersetzungen mit der modellierten Beziehung umlautByBaseToken.
/// Großbuchstaben werden aus den Kleinbuchstaben abgeleitet, die Ersetzung von ß wird gesondert geprüft.
/// Da GermanUmlautCharacterMap intern ist, liest der Test seine Tabellen per Reflection.
/// </summary>
public sealed class GermanUmlautMapLexiconDriftGuardTests
{
    private const string UmlautMapTypeName = "Sprechen.Speech.App.Contracts.Services.GermanUmlautCharacterMap";

    [Fact]
    public void LowercaseDiacriticsMap_EqualsLexiconUmlautByBaseToken_Bidirectional()
    {
        var lexicon = LexiconUmlautByBaseToken();
        var lowercaseMap = ReadCharacterMap("DiacriticsMap")
            .Where(pair => char.IsLower(pair.Key))
            .ToDictionary(pair => pair.Key.ToString(), pair => pair.Value.ToString(), StringComparer.Ordinal);

        // Die Kleinbuchstabentabelle muss in Schlüsseln und Werten genau dem Lexikon entsprechen.
        AssertSetsEqual(
            lexicon,
            lowercaseMap,
            "lexicon umlautByBaseToken",
            "GermanUmlautCharacterMap.DiacriticsMap (lowercase)");
    }

    [Fact]
    public void UppercaseDiacriticsMap_IsTheUppercaseProjectionOfTheSameRelation()
    {
        var lexicon = LexiconUmlautByBaseToken();
        var expected = lexicon.ToDictionary(
            pair => pair.Key.ToUpperInvariant(),
            pair => pair.Value.ToUpperInvariant(),
            StringComparer.Ordinal);
        var uppercaseMap = ReadCharacterMap("DiacriticsMap")
            .Where(pair => char.IsUpper(pair.Key))
            .ToDictionary(pair => pair.Key.ToString(), pair => pair.Value.ToString(), StringComparer.Ordinal);

        // Die Großbuchstabentabelle muss aus derselben modellierten Kleinbuchstabenbeziehung entstehen.
        AssertSetsEqual(expected, uppercaseMap, "uppercase(umlautByBaseToken)", "DiacriticsMap (uppercase)");
    }

    [Fact]
    public void CollapseMap_KeySetEqualsTheSameBaseSet()
    {
        var lexicon = LexiconUmlautByBaseToken();
        var collapseMap = ReadCharacterMap("CollapseMap");

        // Die Schlüsselmengen der Rückabbildung müssen den kleinen und großen Basiszeichen entsprechen.
        var lowercaseKeys = collapseMap.Keys.Where(char.IsLower).Select(c => c.ToString()).ToHashSet(StringComparer.Ordinal);
        Assert.True(
            lexicon.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(lowercaseKeys),
            $"CollapseMap lowercase keys [{string.Join(", ", lowercaseKeys.Order())}] must equal "
            + $"the lexicon base tokens [{string.Join(", ", lexicon.Keys.Order())}].");

        var allKeys = collapseMap.Keys.Select(c => c.ToString()).ToHashSet(StringComparer.Ordinal);
        var expectedKeys = lexicon.Keys
            .Concat(lexicon.Keys.Select(token => token.ToUpperInvariant()))
            .ToHashSet(StringComparer.Ordinal);
        Assert.True(
            expectedKeys.SetEquals(allKeys),
            $"CollapseMap keys [{string.Join(", ", allKeys.Order())}] must equal the base tokens and their "
            + $"uppercase forms [{string.Join(", ", expectedKeys.Order())}].");
    }

    [Fact]
    public void AsciiCanonicalization_UmlautPairsAreTheInverseOfTheLexiconRelation()
    {
        var lexicon = LexiconUmlautByBaseToken();

        // Die erwartete ASCII-Ersetzung wird aus Basiszeichen plus e abgeleitet, auch für Großbuchstaben.
        foreach (var (baseToken, umlaut) in lexicon)
        {
            Assert.Equal(baseToken + "e", ToAscii(umlaut));
            Assert.Equal(baseToken.ToUpperInvariant() + "e", ToAscii(umlaut.ToUpperInvariant()));
        }
    }

    [Fact]
    public void AsciiCanonicalization_SharpSIsTheOneNonUmlautReplacePair()
    {

        Assert.Equal("ss", ToAscii("ß"));
        Assert.Equal("aesse", ToAscii("äße"));
    }

    private static string ToAscii(string value)
        => DiacriticsCanonicalizer.ApplyStrategy(value, DiacriticsStrategies.PreferAscii);

    private static IReadOnlyDictionary<string, string> LexiconUmlautByBaseToken()
        => EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon().UmlautByBaseToken;

    private static IReadOnlyDictionary<char, char> ReadCharacterMap(string fieldName)
    {
        var mapType = typeof(FormDialogPromptIds).Assembly.GetType(UmlautMapTypeName);
        Assert.True(mapType is not null, $"Type '{UmlautMapTypeName}' not found in the contracts assembly.");

        var field = mapType!.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"Field '{fieldName}' not found on '{UmlautMapTypeName}'.");

        return Assert.IsAssignableFrom<IReadOnlyDictionary<char, char>>(field!.GetValue(null));
    }

    private static void AssertSetsEqual(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual,
        string expectedName,
        string actualName)
    {
        var missing = expected.Keys.Except(actual.Keys, StringComparer.Ordinal).Order().ToArray();
        var extra = actual.Keys.Except(expected.Keys, StringComparer.Ordinal).Order().ToArray();
        var mismatched = expected.Keys
            .Intersect(actual.Keys, StringComparer.Ordinal)
            .Where(key => !string.Equals(expected[key], actual[key], StringComparison.Ordinal))
            .Select(key => $"{key}: {expectedName}='{expected[key]}' vs {actualName}='{actual[key]}'")
            .Order()
            .ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0 && mismatched.Length == 0,
            $"Drift between {expectedName} and {actualName}."
            + Environment.NewLine
            + $"Missing in {actualName}: [{string.Join(", ", missing)}]"
            + Environment.NewLine
            + $"Extra in {actualName}: [{string.Join(", ", extra)}]"
            + Environment.NewLine
            + $"Value mismatches: [{string.Join(", ", mismatched)}]");
    }
}
