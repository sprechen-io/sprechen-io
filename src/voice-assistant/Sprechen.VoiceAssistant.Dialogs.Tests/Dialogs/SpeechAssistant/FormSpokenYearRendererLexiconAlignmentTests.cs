using System.Reflection;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht die deutschen und englischen Zahlworttabellen für Einer, Zehner und
/// die Zahlen von elf bis neunzehn des <see cref="FormSpokenYearRenderer"/>
/// mit den entsprechenden Lexikongruppen. Die normalisierten Ausgabewörter müssen
/// darin enthalten sein; zusätzliche Eingabevarianten wie ein oder zwo sind erlaubt.
/// Zusammengesetzte Jahresausdrücke werden hier nicht geparst.
/// </summary>
public sealed class FormSpokenYearRendererLexiconAlignmentTests
{
    [Fact]
    public void YearRendererTables_MatchTheLexiconNumberWords()
    {
        var lexicon = EmbeddedFormTranscriptLexiconProvider.Instance.GetLexicon();

        // Jedes normalisierte Tabellenwort muss in der zugehörigen Lexikongruppe vorkommen.
        AssertGroupIsCovered("GermanUnits", lexicon.NumberWordsByLanguage["de"].Units.Keys);
        AssertGroupIsCovered("GermanTeens", lexicon.NumberWordsByLanguage["de"].Teens.Keys);
        AssertGroupIsCovered("GermanTens", lexicon.NumberWordsByLanguage["de"].Tens.Keys);
        AssertGroupIsCovered("EnglishUnits", lexicon.NumberWordsByLanguage["en"].Units.Keys);
        AssertGroupIsCovered("EnglishTeens", lexicon.NumberWordsByLanguage["en"].Teens.Keys);
        AssertGroupIsCovered("EnglishTens", lexicon.NumberWordsByLanguage["en"].Tens.Keys);
    }

    private static void AssertGroupIsCovered(string fieldName, IReadOnlyCollection<string> lexiconKeys)
    {
        var field = typeof(FormSpokenYearRenderer).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Renderer table '{fieldName}' not found.");
        var rendererWords = (string[])field.GetValue(null)!;
        var foldedLexiconKeys = lexiconKeys
            .Select(key => TextNormalizer.NormalizeForPhraseMatching(key))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var word in rendererWords.Where(word => word.Length > 0))
        {
            var foldedWord = TextNormalizer.NormalizeForPhraseMatching(word);
            Assert.True(
                foldedLexiconKeys.Contains(foldedWord),
                $"Renderer word '{word}' (table {fieldName}) is not in the lexicon's number words.");
        }
    }
}
