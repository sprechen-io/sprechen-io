using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt den Parser für kanonische Formularwerte anhand von Wertart,
/// Code-Stil und bevorzugtem Eingabemodus. Sprachbezogene Interpretation
/// und Nachnormalisierung werden an die zuständigen Parser delegiert.
/// </summary>
internal sealed class FormValueParser : IFormValueParser
{
    private readonly PreferredInputModeValueParser _preferredInputModeParser;
    private readonly EffectiveInteractionValueParser _effectiveInteractionParser;
    private readonly string _requestedIsoCode;

    public FormValueParser(string requestedIsoCode, IFormTranscriptParser transcriptParser)
        : this(requestedIsoCode, transcriptParser, DiacriticsStrategies.Preserve)
    {
    }

    public FormValueParser(
        string requestedIsoCode,
        IFormTranscriptParser transcriptParser,
        string diacriticsStrategy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedIsoCode);
        ArgumentNullException.ThrowIfNull(transcriptParser);

        _requestedIsoCode = requestedIsoCode.Trim();
        var normalizedDiacriticsStrategy = DiacriticsStrategies.NormalizeOrDefault(diacriticsStrategy);
        _preferredInputModeParser = new PreferredInputModeValueParser(transcriptParser, normalizedDiacriticsStrategy);
        _effectiveInteractionParser = new EffectiveInteractionValueParser(_requestedIsoCode, transcriptParser);
    }

    /// <summary>
    /// Versucht eine typbezogene Interpretation mit optionalem Code-Stil
    /// und liefert bei Erfolg den kanonischen Wert.
    /// </summary>
    public bool TryParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        out string canonicalValue)
    {
        return TryParseInput(
            codeStyle,
            valueTypeKind,
            transcript,
            choiceOptions,
            preferredInputModes: null,
            out canonicalValue);
    }

    public bool TryParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        IReadOnlyList<string>? preferredInputModes,
        out string canonicalValue)
    {
        var outcome = ParseInput(codeStyle, valueTypeKind, transcript, choiceOptions, preferredInputModes);
        canonicalValue = outcome.CanonicalValue ?? string.Empty;
        return outcome.Success;
    }

    public FormTranscriptParseOutcome ParseInput(
        bool codeStyle,
        string valueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions,
        IReadOnlyList<string>? preferredInputModes)
    {
        var normalizedValueTypeKind = string.IsNullOrWhiteSpace(valueTypeKind) ? ValueTypeKinds.String : valueTypeKind.Trim();
        var effectiveInteractionType = FieldInteractionSemantics.ResolveEffectiveInteractionType(codeStyle, normalizedValueTypeKind);
        var expectedInputMode = InputModes.ResolveExpectedInputMode(preferredInputModes);

        var outcome = _preferredInputModeParser.Parse(
            _requestedIsoCode,
            expectedInputMode,
            effectiveInteractionType,
            normalizedValueTypeKind,
            transcript,
            out var preferredModeHandled);

        if (preferredModeHandled)
        {
            return outcome;
        }

        return _effectiveInteractionParser.Parse(
            effectiveInteractionType,
            normalizedValueTypeKind,
            transcript,
            choiceOptions);
    }

    /// <summary>
    /// Normalisiert einen bereits zusammengesetzten Buchstabierwert nach,
    /// ohne ihn erneut als Buchstabieräußerung zu interpretieren.
    /// </summary>
    public string NormalizeComposedSpellingValue(bool codeStyle, string valueTypeKind, string composedValue)
    {
        var normalizedValueTypeKind = string.IsNullOrWhiteSpace(valueTypeKind) ? ValueTypeKinds.String : valueTypeKind.Trim();
        return _preferredInputModeParser.NormalizeComposedSpellingValue(
            codeStyle,
            normalizedValueTypeKind,
            composedValue);
    }

    public bool IsComposedValueCompatibleWithType(string valueTypeKind, string composedValue)
    {
        var normalizedValueTypeKind = string.IsNullOrWhiteSpace(valueTypeKind) ? ValueTypeKinds.String : valueTypeKind.Trim();
        return RawFormValueParser.Default.TryParse(composedValue, normalizedValueTypeKind, out _);
    }

    /// <summary>
    /// Liest einen Rohwert entsprechend der Modellwertart als Formularwert.
    /// </summary>
    internal static bool TryParseFormValue(string raw, string valueTypeKind, out FormValue value)
    {
        return RawFormValueParser.Default.TryParse(raw, valueTypeKind, out value);
    }
}
