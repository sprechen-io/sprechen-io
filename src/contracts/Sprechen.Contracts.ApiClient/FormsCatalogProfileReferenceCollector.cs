using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Sammelt unterschiedliche Referenzen auf Auswahlprofile, damit mehrfach referenzierte Profile nicht mehrfach angefordert werden.
/// </summary>
internal sealed class FormsCatalogProfileReferenceCollector
{
    public IReadOnlyCollection<ApplicationSelectionDialogProfileKey> Collect(FormsCatalogDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var keys = new HashSet<ApplicationSelectionDialogProfileKey>();
        Add(keys, document.SelectionDialogProfile);

        foreach (var application in document.Applications)
        {
            Add(keys, application.SelectionDialogProfile);
        }

        return keys;
    }

    private static void Add(
        ISet<ApplicationSelectionDialogProfileKey> keys,
        ApplicationSelectionDialogProfileReference? reference)
    {
        if (reference is not null)
        {
            keys.Add(new ApplicationSelectionDialogProfileKey(reference.ProfileId, reference.ProfileVersion));
        }
    }
}
