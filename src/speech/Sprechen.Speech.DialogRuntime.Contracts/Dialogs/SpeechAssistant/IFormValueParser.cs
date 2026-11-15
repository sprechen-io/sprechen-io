using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Parst ein Feldtranskript in den kanonischen Speicherwert. Getrennt von der Erfassung, damit
/// Buchstabieren und Diktat denselben Typvertrag treffen ohne die Buchstabengrammatik auf
/// schon zusammengesetzte Werte anzuwenden.
/// </summary>
public interface IFormValueParser
{
    FormTranscriptParseOutcome ParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        IReadOnlyList<string>? preferredInputModes);

    bool TryParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        out string canonicalValue);

    bool TryParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        IReadOnlyList<string>? preferredInputModes,
        out string canonicalValue);

    /// <summary>
    /// Normalisiert einen bereits zusammengesetzten Buchstabierwert nach der
    /// Diakritikstrategie, Codeform und Leerraumbehandlung. Die Buchstabiergrammatik
    /// wird nicht erneut angewendet, damit Großschreibung und bereits gebildete
    /// Wörter nicht als neue Buchstabieranweisungen interpretiert werden.
    /// </summary>
    string NormalizeComposedSpellingValue(bool codeStyle, string valueTypeKind, string composedValue);

    /// <summary>
    /// Prüft, ob ein zusammengesetzter Buchstabier-, Ziffern- oder Literalwert
    /// die Form des gebundenen Werttyps erfüllt. Die Buchstaben- oder Zifferngrammatik
    /// wird dabei nicht erneut angewendet.
    /// </summary>
    bool IsComposedValueCompatibleWithType(string valueTypeKind, string composedValue);
}

