namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt die sprachbezogene Ja-Nein-Klassifikation in kanonische
/// boolesche Formularwerte oder ein entsprechendes Fehlerergebnis.
/// </summary>
internal sealed class FormYesNoTranscriptParser
{
    private readonly IYesNoTranscriptClassifier _classifier;

    public FormYesNoTranscriptParser(IYesNoTranscriptClassifier classifier)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    public FormTranscriptParseOutcome Parse(string requestedIsoCode, string transcript)
        => _classifier.Classify(requestedIsoCode, transcript) switch
        {
            YesNoClassification.Yes => FormTranscriptParseOutcome.Succeeded("true"),
            YesNoClassification.No => FormTranscriptParseOutcome.Succeeded("false"),
            YesNoClassification.Ambiguous => FormTranscriptParseOutcome.Failed(
                new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.AmbiguousYesNo)),
            _ => FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized)
        };
}
