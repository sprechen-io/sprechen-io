using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Nichtverstehen zusammen mit einem angehängten Ja-Wort.
/// So genügt das bloße Fehlen eines bekannten Antwortworts im ursprünglichen Transkript
/// nicht für NotRecognized. Weitere Ablehnungsgründe muss die jeweilige Fixture ausschließen.
/// </summary>
internal static class SessionEntryNotUnderstoodProbe
{
    public static bool IsMarker(string transcript)
    {
        var classifier = new YesNoTranscriptClassifier();
        var withYes = $"{transcript} ja";

        return classifier.Classify(VoiceInterfaceModelCatalog.SupportedLanguages, withYes)
            == YesNoClassification.NotRecognized;
    }
}
