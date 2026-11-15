using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Ermöglicht die eigenständige Vorprüfung des Anwendungsauswahldialogs, damit Sprachressourcen und
/// Auswahlvokabular vor der Verwendung geprüft werden.
/// </summary>
public static class ApplicationSelectionDialogPreflight
{
    /// <summary>
    /// Prüft ein eigenständiges Modell der Anwendungsauswahl.
    /// </summary>
    public static PreflightResult Validate(ApplicationSelectionDialog model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new ApplicationSelectionDialogPreflightValidator().Validate(model);
    }
}
