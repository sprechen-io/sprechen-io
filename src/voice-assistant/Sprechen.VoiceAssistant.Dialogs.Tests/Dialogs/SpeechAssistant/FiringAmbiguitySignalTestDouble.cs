namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Löst für den Fehlertest immer ein Mehrdeutigkeitssignal aus.
// Die Unterklasse wird über den Resolver-Konstruktor übergeben,
// weil Moq die interne Produktionsklasse hier nicht ersetzen kann.
internal sealed class FiringAmbiguitySignalTestDouble()
    : FormVoiceCommandAmbiguousSignalDetector(
        new FormVoiceCommandAmbiguitySignalEligibility(),
        new FormVoiceCommandAmbiguityLabelResolver())
{
    public override bool TryDetect(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation,
        out FormVoiceCommandAmbiguitySignal signal)
    {
        signal = new FormVoiceCommandAmbiguitySignal("hilfe");
        return true;
    }
}
