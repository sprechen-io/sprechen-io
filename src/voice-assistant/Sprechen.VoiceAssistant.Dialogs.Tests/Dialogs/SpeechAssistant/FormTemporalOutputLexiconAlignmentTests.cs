using System.Collections;
using System.Reflection;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft per Reflexion die statischen Sammlungsfelder zweier Klassen zur Ausgabe
/// von Zeit- und Datumswerten. Nur die sechs benannten Zahlworttabellen der Jahresausgabe
/// sind zugelassen; ihre Lexikonzugehörigkeit prüft <see cref="FormSpokenYearRendererLexiconAlignmentTests"/>.
/// Andere Formen fest eingebauten Vokabulars werden durch diese Strukturprüfung nicht erfasst.
/// </summary>
public sealed class FormTemporalOutputLexiconAlignmentTests
{
    // Erlaubte Zahlworttabellen der Jahresausgabe; weitere statische Sammlungsfelder werden abgelehnt.
    private static readonly IReadOnlySet<string> SubsetBoundNumberWordTables =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "GermanUnits", "GermanTeens", "GermanTens", "EnglishUnits", "EnglishTeens", "EnglishTens",
        };

    [Fact]
    public void SpokenValueResolver_HasNoHandWrittenVocabularyTables()
    {
        var vocabularyFields = VocabularyTableFields(typeof(FormTemporalSpokenValueResolver)).ToList();

        Assert.True(
            vocabularyFields.Count == 0,
            $"FormTemporalSpokenValueResolver must not carry hand-written vocabulary tables; found: {string.Join(", ", vocabularyFields)}.");
    }

    [Fact]
    public void YearRenderer_HasNoVocabularyTablesBeyondTheSubsetBoundNumberWords()
    {
        var allTables = VocabularyTableFields(typeof(FormSpokenYearRenderer)).ToList();
        var replicaTables = allTables
            .Where(field => !SubsetBoundNumberWordTables.Contains(field))
            .ToList();


        Assert.True(
            replicaTables.Count == 0,
            $"FormSpokenYearRenderer must not carry hand-written vocabulary tables beyond the subset-bound number words; found: {string.Join(", ", replicaTables)}.");

        // Die erwarteten sechs Tabellen müssen vorhanden sein; so bleibt die Ausnahmeliste überprüfbar.
        Assert.Equal(
            SubsetBoundNumberWordTables.Order(StringComparer.Ordinal),
            allTables.Order(StringComparer.Ordinal));
    }

    private static IEnumerable<string> VocabularyTableFields(Type rendererType)
    {
        // Erfasst statische Felder mit IEnumerable-Typ, außer string.
        // Der Inhalt der Sammlung wird nicht untersucht.
        return rendererType
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => field.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(field.FieldType))
            .Select(field => field.Name);
    }
}
