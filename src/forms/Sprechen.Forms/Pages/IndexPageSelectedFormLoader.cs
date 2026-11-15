using Sprechen.Forms.Services;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Lädt den Vertrag des gewählten Formulars über den MDA-Datenclient
/// und erzeugt die Anzeigedaten oder einen Fehlercode.
/// </summary>
internal sealed class IndexPageSelectedFormLoader
{
    private readonly IndexPageSelectedFormLoadErrorMapper _errorMapper;
    private readonly IMdaFormContractDataClient _formContractClient;
    private readonly IndexPageSelectedFormProjectionBuilder _projectionBuilder;

    public IndexPageSelectedFormLoader(
        IMdaFormContractDataClient formContractClient,
        IndexPageSelectedFormProjectionBuilder projectionBuilder,
        IndexPageSelectedFormLoadErrorMapper errorMapper)
    {
        _formContractClient = formContractClient ?? throw new ArgumentNullException(nameof(formContractClient));
        _projectionBuilder = projectionBuilder ?? throw new ArgumentNullException(nameof(projectionBuilder));
        _errorMapper = errorMapper ?? throw new ArgumentNullException(nameof(errorMapper));
    }

    public async Task<IndexPageSelectedFormResult> LoadAsync(
        IndexPageSelectedFormRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var selectedForm = request.SelectedForm;
            var contract = await _formContractClient.GetFormContractAsync(
                    request.SelectedApplicationId,
                    selectedForm.Id,
                    selectedForm.ContractVersion,
                    cancellationToken)
                .ConfigureAwait(false);

            return _projectionBuilder.Build(request, contract);
        }
        catch (Exception ex)
        {
            return _errorMapper.Map(ex);
        }
    }
}
