using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Referenzen vom Anwendungskatalog auf Auswahldialog und Dialogprofile.
/// </summary>
public static class FormsCatalogDocumentPreflight
{
    /// <summary>
    /// Prüft Kennung und Version des Auswahldialogs sowie das Vorhandensein referenzierter Profile.
    /// </summary>
    public static PreflightResult Validate(
        FormsCatalogDocument document,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectionDialog);
        ArgumentNullException.ThrowIfNull(profilesByKey);

        return new FormsCatalogDocumentPreflightValidator()
            .Validate(document, selectionDialog, profilesByKey);
    }
}
