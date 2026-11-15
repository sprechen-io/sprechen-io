using System.Collections;
using System.Reflection;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft ausgewählte Datums-, Zeit- und Zahlwortparser auf statische Sammlungsfelder.
/// Zusätzlich dürfen zwei Datumsparser keine Felder namens MinSpelledYear oder
/// MaxSpelledYear deklarieren; die gemeinsamen Konstanten werden auf 1900 und 2100 geprüft.
/// Andere Formen doppelten Vokabulars oder doppelter Grenzwerte erfasst dies nicht.
/// </summary>
public sealed class FormTemporalParserLexiconAlignmentTests
{
    [Theory]
    [InlineData(typeof(FormSpokenDateParser))]
    [InlineData(typeof(FormEnglishSpokenDateParser))]
    [InlineData(typeof(FormRomanceSpokenDateParser))]
    [InlineData(typeof(FormSpokenTimeParser))]
    [InlineData(typeof(FormSpokenNumberWordParser))]
    public void TemporalParsers_HaveNoHandWrittenVocabularyTables(Type parserType)
    {
        // Erfasst statische IEnumerable-Felder außer string. Einzelne Zeichenketten
        // und andere Speicherformen werden durch diese Prüfung nicht untersucht.
        var vocabularyFields = parserType
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(field.FieldType))
            .Select(field => $"{field.FieldType.Name} {field.Name}")
            .ToList();

        Assert.True(
            vocabularyFields.Count == 0,
            $"{parserType.Name} must not carry hand-written vocabulary tables; found: {string.Join(", ", vocabularyFields)}.");
    }

    [Theory]
    [InlineData(typeof(FormSpokenDateParser))]
    [InlineData(typeof(FormEnglishSpokenDateParser))]
    public void SpelledYearRangeConstants_AreNotDuplicatedInTheParsers(Type parserType)
    {

        Assert.Null(parserType.GetField("MinSpelledYear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.Null(parserType.GetField("MaxSpelledYear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        Assert.Equal(1900, FormTemporalTranscriptParser.MinSpelledYear);
        Assert.Equal(2100, FormTemporalTranscriptParser.MaxSpelledYear);
    }
}
