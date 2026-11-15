namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Löst ein <see cref="FormsCatalogDocument"/> mit referenzierten Auswahldialog-Modellen
/// und Profilen in die Laufzeitdarstellung <see cref="FormsCatalog"/> auf.
/// </summary>
public static class FormsCatalogResolver
{
    /// <summary>
    /// Löst ein Anwendungskatalog-Dokument samt referenzierten Auswahldialog-Artefakten in den wirksamen Laufzeitkatalog auf.
    /// </summary>
    public static FormsCatalog Resolve(
        FormsCatalogDocument document,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectionDialog);
        ArgumentNullException.ThrowIfNull(profilesByKey);

        return new FormsCatalogDocumentResolver()
            .Resolve(document, selectionDialog, profilesByKey);
    }
}
