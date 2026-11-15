namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Ja/Nein-Deutung eines Transkripts. Der Dialog kann damit nicht zuordenbare,
/// eindeutige und widersprüchliche Antworten unterscheiden.
/// </summary>
public enum YesNoClassification
{
    NotRecognized,
    No,
    Yes,
    Ambiguous
}
