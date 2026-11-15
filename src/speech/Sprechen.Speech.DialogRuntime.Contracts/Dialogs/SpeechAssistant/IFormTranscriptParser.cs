using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert erkannte Sprache als typisierte Formulareingabe einschließlich Auswahl und Buchstabierung.
/// </summary>
public interface IFormTranscriptParser
{
    FormTranscriptParseOutcome ParseYesNo(string requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseInteger(string? requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseDigits(string? requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseSpelling(string? requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseDate(string requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseTime(string requestedIsoCode, string transcript);

    FormTranscriptParseOutcome ParseChoice(
        string? requestedIsoCode,
        string transcript,
        ImmutableArray<FormChoiceOption> options,
        bool allowMultiple);

    string NormalizeCode(string transcript);
}
