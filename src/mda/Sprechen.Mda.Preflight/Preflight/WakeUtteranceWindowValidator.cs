using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft das Zeitfenster und die zulässige Anzahl vorheriger Aufnahmezyklen
/// beim Zusammenführen einer Aktivierungsäußerung.
/// </summary>
internal sealed class WakeUtteranceWindowValidator
{
    public void Validate(List<PreflightIssue> errors, WakeUtteranceWindow? window)
    {
        if (window is null)
        {
            return;
        }

        ValidateInterCyclePause(errors, window);
        ValidateCarriedCycles(errors, window);
    }

    private static void ValidateInterCyclePause(List<PreflightIssue> errors, WakeUtteranceWindow window)
    {
        if (window.MaxInterCyclePauseMs >= 0 && window.MaxInterCyclePauseMs <= 60_000)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: "WakeUtteranceWindow.maxInterCyclePauseMs must be between 0 and 60000.",
            Path: "$.wakeUtteranceWindow.maxInterCyclePauseMs"));
    }

    private static void ValidateCarriedCycles(List<PreflightIssue> errors, WakeUtteranceWindow window)
    {
        // Die Laufzeit übernimmt nur einen vorherigen Aufnahmezyklus.
        // Größere konfigurierte Anzahlen würden eine nicht unterstützte Funktion versprechen.
        if (window.MaxCarriedCycles == 1)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: "WakeUtteranceWindow.maxCarriedCycles must be 1 (only single-cycle carry is supported).",
            Path: "$.wakeUtteranceWindow.maxCarriedCycles"));
    }
}
