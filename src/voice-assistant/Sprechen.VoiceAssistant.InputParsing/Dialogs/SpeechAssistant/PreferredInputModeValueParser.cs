using System.Globalization;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert Eingaben gemäß bevorzugtem Modus und Wertart
/// und normalisiert zusammengesetzte Buchstabierwerte für die weitere Verarbeitung.
/// </summary>
internal sealed class PreferredInputModeValueParser
{
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly string _diacriticsStrategy;

    public PreferredInputModeValueParser(
        IFormTranscriptParser transcriptParser,
        string diacriticsStrategy)
    {
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _diacriticsStrategy = DiacriticsStrategies.NormalizeOrDefault(diacriticsStrategy);
    }

    public FormTranscriptParseOutcome Parse(
        string? requestedIsoCode,
        InputMode expectedInputMode,
        string effectiveInteractionType,
        string normalizedValueTypeKind,
        string transcript,
        out bool handled)
    {
        handled = true;

        if (expectedInputMode == InputMode.Literal
            && string.Equals(normalizedValueTypeKind, ValueTypeKinds.String, StringComparison.OrdinalIgnoreCase))
        {
            var literal = transcript.Trim();
            return literal.Length > 0
                ? FormTranscriptParseOutcome.Succeeded(literal)
                : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        if (expectedInputMode == InputMode.Digits)
        {
            return ParseDigits(requestedIsoCode, normalizedValueTypeKind, transcript, out handled);
        }

        if (expectedInputMode != InputMode.Spelling
            || !string.Equals(normalizedValueTypeKind, ValueTypeKinds.String, StringComparison.OrdinalIgnoreCase))
        {
            handled = false;
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        return ParseSpelling(requestedIsoCode, effectiveInteractionType, transcript);
    }

    private FormTranscriptParseOutcome ParseDigits(
        string? requestedIsoCode,
        string normalizedValueTypeKind,
        string transcript,
        out bool handled)
    {
        // Der Ziffernmodus behandelt nur Zeichenketten und Ganzzahlen. Bei strukturierten
        // Wertarten wie Datum oder Uhrzeit bleibt die typbezogene Interpretation zuständig.
        var digitsModeOwnsValue =
            string.Equals(normalizedValueTypeKind, ValueTypeKinds.String, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedValueTypeKind, ValueTypeKinds.Integer, StringComparison.OrdinalIgnoreCase);

        if (!digitsModeOwnsValue)
        {
            handled = false;
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        handled = true;

        var outcome = _transcriptParser.ParseDigits(requestedIsoCode, transcript);
        if (!outcome.Success)
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var digits = outcome.CanonicalValue!;

        if (string.Equals(normalizedValueTypeKind, ValueTypeKinds.Integer, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)
                ? FormTranscriptParseOutcome.Succeeded(integer.ToString(CultureInfo.InvariantCulture))
                : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        return FormTranscriptParseOutcome.Succeeded(digits);
    }

    /// <summary>
    /// Wendet die Diakritikstrategie und gegebenenfalls die Code-Normalisierung auf einen
    /// bereits zusammengesetzten Buchstabierwert an und entfernt äußere Leerzeichen.
    /// Die Buchstabiergrammatik wird nicht erneut ausgeführt, damit Großbuchstaben
    /// und mit Tabellenwörtern kollidierende Werte erhalten bleiben.
    /// </summary>
    public string NormalizeComposedSpellingValue(bool codeStyle, string normalizedValueTypeKind, string composedValue)
    {
        var effectiveInteractionType = FieldInteractionSemantics.ResolveEffectiveInteractionType(codeStyle, normalizedValueTypeKind);
        return NormalizeComposedSpellingValue(effectiveInteractionType, composedValue);
    }

    private string NormalizeComposedSpellingValue(string effectiveInteractionType, string spellingValue)
    {
        spellingValue = DiacriticsCanonicalizer.ApplyStrategy(spellingValue, _diacriticsStrategy);

        if (string.Equals(effectiveInteractionType, EffectiveFieldInteractionTypes.Code, StringComparison.OrdinalIgnoreCase))
        {
            spellingValue = _transcriptParser.NormalizeCode(spellingValue);
        }

        return spellingValue.Trim();
    }

    private FormTranscriptParseOutcome ParseSpelling(
        string? requestedIsoCode,
        string effectiveInteractionType,
        string transcript)
    {
        var outcome = _transcriptParser.ParseSpelling(requestedIsoCode, transcript);
        if (!outcome.Success)
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var spellingValue = NormalizeComposedSpellingValue(effectiveInteractionType, outcome.CanonicalValue!);

        return spellingValue.Length > 0
            ? FormTranscriptParseOutcome.Succeeded(spellingValue)
            : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }
}
