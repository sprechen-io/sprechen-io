using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Erkennt nur die vorgegebenen Wörter als Zustimmung.
// So hängen die Tests nicht von Änderungen am ausgelieferten Lexikon ab.
internal sealed class FakeYesNoClassifier(params string[] known) : IYesNoTranscriptClassifier
{
    public YesNoClassification Classify(string? requestedIsoCode, string transcript)
        => known.Contains(transcript, StringComparer.OrdinalIgnoreCase)
            ? YesNoClassification.Yes
            : YesNoClassification.NotRecognized;
}
