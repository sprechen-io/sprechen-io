namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bietet eine verneinungsbewusste Ja-Nein-Klassifikation, damit Parser und Dialogsteuerung
/// Transkripte nach denselben Regeln deuten.
/// </summary>
public interface IYesNoTranscriptClassifier
{
    YesNoClassification Classify(string? requestedIsoCode, string transcript);
}
