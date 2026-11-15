namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Zustimmung, Ablehnung und Abbruch bei der Bestätigung eines Sprachwechsels
/// sowie das Erreichen der Höchstzahl von Aufnahmefehlern. Nach Ablehnung kann der
/// Katalogdialog weiterlaufen. Technische Fehler und ausdrückliche Abbrüche bleiben unterscheidbar.
/// </summary>
internal enum FormsCatalogLanguageChangeDecision
{
    Confirmed,
    Declined,
    Cancelled,
    CaptureFaultBudgetExhausted
}
