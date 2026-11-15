using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Bündelt die referenzierten Dialog- und Profilschlüssel, damit weitere Prüfungen denselben benötigten Bestand verwenden.
/// </summary>
internal sealed record ApplicationSelectionDialogBundleConsistencyResult(
    SelectionDialogKey SelectionDialogKey,
    IReadOnlySet<ApplicationSelectionDialogProfileKey> ReferencedProfileKeys);
