using Sprechen.Contracts.MdaFormAccess;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Führt Katalog, Auswahldialog und Profile zusammen und prüft sie, damit die Oberfläche einen vollständig aufgelösten Katalog erhält.
/// </summary>
public sealed class FormsCatalogTypedClient : IFormsCatalogDataClient
{
    private const string RequiredSelectionDialogModelVersion = "5";

    private readonly FormsCatalogDocumentResourceClient _catalogDocumentClient;
    private readonly ApplicationSelectionDialogResourceClient _selectionDialogClient;
    private readonly ApplicationSelectionDialogProfileResourceClient _selectionDialogProfileClient;
    private readonly FormsCatalogProfileReferenceCollector _profileReferenceCollector;
    private readonly ContractsApiPreflightFailureMapper _preflightFailureMapper;

    internal FormsCatalogTypedClient(
        FormsCatalogDocumentResourceClient catalogDocumentClient,
        ApplicationSelectionDialogResourceClient selectionDialogClient,
        ApplicationSelectionDialogProfileResourceClient selectionDialogProfileClient,
        FormsCatalogProfileReferenceCollector profileReferenceCollector,
        ContractsApiPreflightFailureMapper preflightFailureMapper)
    {
        _catalogDocumentClient = catalogDocumentClient ?? throw new ArgumentNullException(nameof(catalogDocumentClient));
        _selectionDialogClient = selectionDialogClient ?? throw new ArgumentNullException(nameof(selectionDialogClient));
        _selectionDialogProfileClient = selectionDialogProfileClient ?? throw new ArgumentNullException(nameof(selectionDialogProfileClient));
        _profileReferenceCollector = profileReferenceCollector ?? throw new ArgumentNullException(nameof(profileReferenceCollector));
        _preflightFailureMapper = preflightFailureMapper ?? throw new ArgumentNullException(nameof(preflightFailureMapper));
    }

    public Task<FormsCatalog> GetFormsCatalogAsync(CancellationToken cancellationToken)
    {
        // Der Katalog kann nach einer Veröffentlichung andere Ressourcen referenzieren.
        // Deshalb wird er bei jedem Aufruf neu geladen. Nur unveränderliche,
        // über ihre Version adressierte Ressourcen werden zwischengespeichert.
        return LoadFormsCatalogAsync(cancellationToken);
    }

    private async Task<FormsCatalog> LoadFormsCatalogAsync(CancellationToken cancellationToken)
    {
        var document = await _catalogDocumentClient.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!string.Equals(
                document.SelectionDialog.ModelVersion,
                RequiredSelectionDialogModelVersion,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"This Web release requires application selection dialog model version '{RequiredSelectionDialogModelVersion}', " +
                $"but the active Contracts catalog references '{document.SelectionDialog.ModelVersion}'. Reload after the release cutover.");
        }

        var selectionDialog = await _selectionDialogClient
            .GetAsync(
                document.SelectionDialog.SelectionDialogId,
                document.SelectionDialog.ModelVersion,
                cancellationToken)
            .ConfigureAwait(false);
        var profiles = await _selectionDialogProfileClient
            .LoadProfilesAsync(_profileReferenceCollector.Collect(document), cancellationToken)
            .ConfigureAwait(false);

        _preflightFailureMapper.ThrowInvalidOperationIfFailed(
            FormsCatalogDocumentPreflight.Validate(document, selectionDialog, profiles),
            "FormsCatalog document");

        var catalog = FormsCatalogResolver.Resolve(document, selectionDialog, profiles);
        _preflightFailureMapper.ThrowInvalidOperationIfFailed(
            FormsCatalogPreflight.Validate(catalog),
            "FormsCatalog");
        return catalog;
    }
}
