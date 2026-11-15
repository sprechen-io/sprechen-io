namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet Start, geladenen Aktivierungsbestand und fehlgeschlagenen Aufbau, damit Bereitschaft nicht aus dem Start allein abgeleitet wird.
/// </summary>
public enum WakeWordListenerSetupPhase
{
    Started = 0,
    InventoryLoaded = 1,
    Failed = 2
}
