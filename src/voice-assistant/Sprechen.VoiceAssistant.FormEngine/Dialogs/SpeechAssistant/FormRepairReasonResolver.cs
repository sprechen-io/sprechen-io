using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt den Rückmeldungsgrund aus abgelehnter Erkennungskonfidenz, Transkript und Parserkategorie.
/// Unbekannte Parserkategorien werden bei der Zuordnung zurückgewiesen.
/// </summary>
public sealed class FormRepairReasonResolver
{
    private readonly IYesNoTranscriptClassifier? _yesNoClassifier;

    /// <summary>
    /// Erlaubt die Auflösung ohne Wortschatzklassifizierer. Die Zusatzkategorie
    /// <see cref="FormRepairReason.KnownVocabularyInvalidHere"/> wird dann nicht abgeleitet.
    /// </summary>
    public FormRepairReasonResolver(IYesNoTranscriptClassifier? yesNoClassifier = null)
    {
        _yesNoClassifier = yesNoClassifier;
    }

    /// <summary>
    /// Eine bereits wegen ihrer Konfidenz abgelehnte Erkennung hat Vorrang vor Transkript
    /// und Parserkategorie. Die Methode wertet den Konfidenzwert nicht selbst aus.
    /// </summary>
    public FormRepairReason Resolve(
        FormTranscriptParseFailure? parseFailure,
        bool confidenceRejected,
        string? transcript,
        string? isoCode = null)
    {
        if (confidenceRejected)
        {
            return FormRepairReason.ConfidenceRejected;
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            return FormRepairReason.NothingCaptured;
        }

        if (parseFailure is null)
        {
            return Refine(FormRepairReason.UnparsedUtterance, transcript, isoCode);
        }

        return Refine(MapCategory(parseFailure), transcript, isoCode);
    }

    /// <summary>
    /// Präzisiert zwei unspezifische Parserergebnisse anhand des sprachbezogenen
    /// Ja-Nein-Wortschatzes. Bereits spezifischere Kategorien behalten ihren Grund.
    /// </summary>
    private FormRepairReason Refine(FormRepairReason reason, string? transcript, string? isoCode)
    {
        if (reason is not (FormRepairReason.UnparsedUtterance or FormRepairReason.NoMatchingOption)
            || _yesNoClassifier is null
            || string.IsNullOrWhiteSpace(isoCode)
            || string.IsNullOrWhiteSpace(transcript))
        {
            return reason;
        }

        var classification = _yesNoClassifier.Classify(isoCode, transcript);
        return classification is YesNoClassification.Yes or YesNoClassification.No
            ? FormRepairReason.KnownVocabularyInvalidHere
            : reason;
    }

    private static FormRepairReason MapCategory(FormTranscriptParseFailure parseFailure)
    {
        return parseFailure.Category switch
        {
            FormTranscriptParseFailureCategory.NotRecognized => FormRepairReason.UnparsedUtterance,
            FormTranscriptParseFailureCategory.AmbiguousYesNo => FormRepairReason.AmbiguousYesNo,
            FormTranscriptParseFailureCategory.NoMatchingOption => FormRepairReason.NoMatchingOption,
            FormTranscriptParseFailureCategory.AmbiguousChoice => FormRepairReason.AmbiguousChoice,
            FormTranscriptParseFailureCategory.UnknownSpellingToken => FormRepairReason.UnknownSpellingToken,
            FormTranscriptParseFailureCategory.NumberOutOfRange => FormRepairReason.ValueOutOfRange,
            FormTranscriptParseFailureCategory.InvalidCalendarDate => FormRepairReason.ValueOutOfRange,
            FormTranscriptParseFailureCategory.TimeOutOfRange => FormRepairReason.ValueOutOfRange,
            FormTranscriptParseFailureCategory.DecimalNotSupported => FormRepairReason.UnsupportedValueForm,
            FormTranscriptParseFailureCategory.MultipleNumbers => FormRepairReason.UnsupportedValueForm,
            FormTranscriptParseFailureCategory.UnsupportedTemporalForm => FormRepairReason.UnsupportedValueForm,
            _ => throw new ArgumentOutOfRangeException(
                nameof(parseFailure),
                parseFailure.Category,
                "Unmapped transcript-parse failure category: add its repair reason instead of letting it "
                + "fall back to another category's spoken wording."),
        };
    }
}
