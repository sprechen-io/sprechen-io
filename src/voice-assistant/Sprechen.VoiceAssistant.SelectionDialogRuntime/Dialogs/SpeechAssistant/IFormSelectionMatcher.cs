namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt den gemeinsamen Kandidatenvergleich für Formulare bereit. Formularspezifische Aufrufer
/// verwenden dadurch dieselben Schnappschuss- und Auflösungsregeln wie die generische Auswahl.
/// </summary>
internal interface IFormSelectionMatcher
{
    SelectionCandidateMatcher<FormInfo> CandidateMatcher { get; }
}

