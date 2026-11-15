using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Bietet Aufrufern eine boolesche Sicht auf die modellbasierte Ja-Nein-Klassifikation. Sprachabhängige Erkennungswörter stammen aus dem gemeinsamen Lexikon.
/// </summary>
public static class YesNo
{
    private static readonly YesNoTranscriptClassifier Classifier = new();

    public static bool TryParseYesNo(string transcript, string isoCode, out bool isYes)
        => TryResolve(Classifier.Classify(isoCode, transcript), out isYes);

    public static bool TryParseYesNo(string transcript, IEnumerable<string> isoCodes, out bool isYes)
    {
        if (isoCodes is null)
        {
            isYes = false;
            return false;
        }

        return TryResolve(Classifier.Classify(isoCodes, transcript), out isYes);
    }

    public static string GetYesWord(string isoCode)
        => VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesWord(isoCode);

    public static string GetNoWord(string isoCode)
        => VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoWord(isoCode);

    private static bool TryResolve(YesNoClassification classification, out bool isYes)
    {
        switch (classification)
        {
            case YesNoClassification.Yes:
                isYes = true;
                return true;
            case YesNoClassification.No:
                isYes = false;
                return true;
            default:
                isYes = false;
                return false;
        }
    }
}
