namespace Sprechen.Forms.Pages;

/// <summary>
/// Steuert den Ladevorgang der Index-Seite: Katalog laden, Studienlink binden, Auswahl auflösen, Formular laden.
/// </summary>
internal sealed class IndexPageWorkflow
{
    private readonly FormsCatalogLoader _catalogLoader;
    private readonly IndexPageRequestReader _requestReader;
    private readonly IndexPageSelectedFormLoader _selectedFormLoader;
    private readonly IndexPageSelectionResolver _selectionResolver;

    public IndexPageWorkflow(
        FormsCatalogLoader catalogLoader,
        IndexPageSelectionResolver selectionResolver,
        IndexPageSelectedFormLoader selectedFormLoader,
        IndexPageRequestReader requestReader)
    {
        ArgumentNullException.ThrowIfNull(catalogLoader);
        ArgumentNullException.ThrowIfNull(selectionResolver);
        ArgumentNullException.ThrowIfNull(selectedFormLoader);
        ArgumentNullException.ThrowIfNull(requestReader);

        _catalogLoader = catalogLoader;
        _selectionResolver = selectionResolver;
        _selectedFormLoader = selectedFormLoader;
        _requestReader = requestReader;
    }

    public async Task<IndexPageWorkflowResult> LoadAsync(
        IndexPageWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Admission.IsOperationalProbe && !string.IsNullOrWhiteSpace(request.Admission.StudyToken))
        {
            throw new InvalidOperationException("A survey probe cannot carry a participant token.");
        }

        var studyParameters = new IndexPageStudyParameters(string.Empty, string.Empty, "A");
        var catalogLoadResult = await _catalogLoader.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!catalogLoadResult.IsSuccess || catalogLoadResult.Catalog is null)
        {
            return IndexPageWorkflowResult.CatalogLoadFailed(studyParameters, catalogLoadResult);
        }

        var selectionRequest = request;
        if (!string.IsNullOrWhiteSpace(request.Admission.StudyToken) || request.Admission.IsOperationalProbe)
        {
            var bound = FormsStudyDirectEntry.Bind(catalogLoadResult.Catalog);
            selectionRequest = new IndexPageWorkflowRequest(
                request.Request,
                request.Language,
                bound.ApplicationId,
                bound.FormId,
                previousApplicationId: null,
                request.Admission);
        }

        var selection = _selectionResolver.Resolve(
            catalogLoadResult.Catalog,
            _requestReader.ReadSelectionRequest(selectionRequest));

        if (selection.SelectedApplication is null || selection.SelectedForm is null)
        {
            return new IndexPageWorkflowResult
            {
                StudyParameters = studyParameters,
                Selection = selection
            };
        }

        var loadedForm = await _selectedFormLoader.LoadAsync(
                _requestReader.ReadSelectedFormRequest(selection),
                cancellationToken)
            .ConfigureAwait(false);

        return new IndexPageWorkflowResult
        {
            StudyParameters = studyParameters,
            Selection = selection,
            LoadedForm = loadedForm
        };
    }
}
