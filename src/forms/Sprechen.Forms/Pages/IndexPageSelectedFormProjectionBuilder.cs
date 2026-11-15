using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Setzt aus dem Formularvertrag das Anzeigeergebnis zusammen: Felder, Gruppen, lokalisierte Texte, Vorschau, Vertrags-JSON.
/// </summary>
internal sealed class IndexPageSelectedFormProjectionBuilder
{
    private readonly IFormSurfaceDefinitionBuilder _fieldBuilder;
    private readonly IFormSurfacePresenter _surfacePresenter;
    private readonly IndexPageFormContractTextResolver _textResolver;

    public IndexPageSelectedFormProjectionBuilder(
        IFormSurfaceDefinitionBuilder fieldBuilder,
        IFormSurfacePresenter surfacePresenter,
        IndexPageFormContractTextResolver textResolver)
    {
        ArgumentNullException.ThrowIfNull(fieldBuilder);
        ArgumentNullException.ThrowIfNull(surfacePresenter);
        ArgumentNullException.ThrowIfNull(textResolver);

        _fieldBuilder = fieldBuilder;
        _surfacePresenter = surfacePresenter;
        _textResolver = textResolver;
    }

    public IndexPageSelectedFormResult Build(IndexPageSelectedFormRequest request, FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(contract);

        var politenessLevel = request.SelectedApplication.VoiceDialogSettings?.GetPolitenessLevel() ?? PolitenessLevel.Formal;
        var htmlProjectionProfile = HtmlProjectionProfileResolver.Resolve(contract.Projections?.Html);
        var formProjection = _fieldBuilder.BuildFormSurfaceDefinition(contract, request.SelectedLanguage, politenessLevel);
        var displayName = ResolveDisplayName(request, contract, politenessLevel);

        return new IndexPageSelectedFormResult
        {
            EntryFieldGroupId = formProjection.EntryFieldGroupId,
            FieldGroupsById = formProjection.FieldGroupsById,
            Fields = formProjection.Fields,
            Surface = _surfacePresenter.PresentPreview(formProjection),
            FormContractJson = JsonSerializer.Serialize(contract),
            FormDisplayName = displayName,
            FormDescription = _textResolver.Resolve(
                contract.Description,
                contract,
                request.SelectedLanguage,
                politenessLevel),
            HtmlProjectionProfile = htmlProjectionProfile
        };
    }

    private string ResolveDisplayName(
        IndexPageSelectedFormRequest request,
        FormContract contract,
        PolitenessLevel politenessLevel)
    {
        var displayName = _textResolver.Resolve(
            contract.DisplayName,
            contract,
            request.SelectedLanguage,
            politenessLevel);

        return string.IsNullOrWhiteSpace(displayName) ? request.SelectedForm.DisplayName : displayName;
    }
}
