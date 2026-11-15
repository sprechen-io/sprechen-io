using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert Erkennungshinweise passend zur Auswahlphase und Bestätigungsstufe.
/// Die Implementierung ermittelt modellierte Äußerungen. Die gemeinsame
/// Auswahllaufzeit verwendet nur diese Schnittstelle. Hinweise unterstützen
/// die Erkennung, ersetzen aber keine Prüfung der erkannten Antwort.
/// </summary>
public interface ISelectionTurnKeyterms
{
    /// <summary>
    /// Liefert Erkennungshinweise für einen Zug, der eine Auswahl erwartet.
    /// </summary>
    SpeechKeyterms? ForListening();

    /// <summary>
    /// Liefert Hinweise für die angegebene Bestätigungsstufe. Spätere Autorisierungsstufen können
    /// einen Öffnungsbefehl mit exaktem Kandidatennamen oder eine Auswahl über die stabile Nummer
    /// verwenden. Die erste Bestätigung nimmt diese Formen nicht an.
    /// </summary>
    SpeechKeyterms? ForConfirming(SelectionConfirmationCaptureStage stage);
}
