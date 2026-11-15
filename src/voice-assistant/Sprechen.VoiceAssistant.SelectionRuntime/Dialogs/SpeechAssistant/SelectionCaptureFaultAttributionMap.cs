using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet bekannte Aufnahmefehler anhand ihrer Codes einer Fehlerkategorie zu.
/// Unbekannte Codes werden vor der Entscheidung über weitere Versuche abgewiesen.
/// Die Auswahllaufzeit verwendet die vorhandenen Ausnahmecodes, ohne sie in einer
/// tieferen Vertragsschicht zu duplizieren.
/// </summary>
internal static class SelectionCaptureFaultAttributionMap
{
    /// <summary>
    /// Prüft, ob der Fehlercode eine hinterlegte Zuordnung besitzt. Unbekannte Codes lösen zur
    /// Laufzeit eine Ausnahme aus, bevor die Budgetentscheidung sie verwenden kann.
    /// </summary>
    public static void EnsureAttributed(string failureCode) => Attribute(failureCode);

    public static SpeechCaptureFaultAttribution Attribute(string failureCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureCode);

        return failureCode switch
        {
            SpeechRecognitionQueueFullException.CodeValue => SpeechCaptureFaultAttribution.Infrastructure,
            SpeechRecognitionBackoffExhaustedException.CodeValue => SpeechCaptureFaultAttribution.Infrastructure,
            SpeechCaptureFailureCodes.CaptureInterrupted => SpeechCaptureFaultAttribution.Infrastructure,

            // Unbekannte Codes werden abgewiesen, statt sie stillschweigend
            // der Fehlergrenze für Teilnehmende zuzuordnen.
            _ => throw new ArgumentOutOfRangeException(
                nameof(failureCode),
                failureCode,
                "This capture failure code has no recorded attribution. State whether it is the "
                + "participant's fault or the infrastructure's before it can spend a retry budget."),
        };
    }
}
