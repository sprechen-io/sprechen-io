using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Ermöglicht die eigenständige Vorprüfung eines Auswahlprofils, damit Zeit-, Wiederholungs- und
/// Konfidenzeinstellungen vor der Verwendung geprüft werden.
/// </summary>
public static class ApplicationSelectionDialogProfilePreflight
{
    /// <summary>
    /// Prüft ein eigenständiges Profil der Anwendungsauswahl.
    /// </summary>
    public static PreflightResult Validate(ApplicationSelectionDialogProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new ApplicationSelectionDialogProfilePreflightValidator().Validate(profile);
    }
}
