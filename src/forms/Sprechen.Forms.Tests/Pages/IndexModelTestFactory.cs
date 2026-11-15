using Sprechen.Contracts.FormSurfaces;
using Sprechen.Forms.Pages;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Forms.Tests.Pages;

internal sealed class IndexModelTestFactory
{
    public IndexPageRuntime CreateRuntime(
        IFormsCatalogDataClient catalogClient,
        IMdaFormContractDataClient formContractClient,
        IFormSurfaceDefinitionBuilder fieldBuilder,
        IFormSurfacePresenter surfacePresenter)
    {
        var catalogLoader = new FormsCatalogLoader(catalogClient);
        var languageResolver = CreateLanguageResolver();
        var applicationOptionsResolver = new IndexPageApplicationOptionsResolver();
        var selectedApplicationResolver = new IndexPageSelectedApplicationResolver();
        var formOptionResolver = new IndexPageFormOptionResolver();
        var selectedFormResolver = new IndexPageSelectedFormResolver();
        var navigationManifestBuilder = new FormsNavigationManifestJsonBuilder(new FormsNavigationManifestProjector());
        var selectionResolver = new IndexPageSelectionResolver(
            languageResolver,
            applicationOptionsResolver,
            selectedApplicationResolver,
            formOptionResolver,
            selectedFormResolver,
            navigationManifestBuilder);
        var selectedFormLoader = new IndexPageSelectedFormLoader(
            formContractClient,
            new IndexPageSelectedFormProjectionBuilder(
                fieldBuilder,
                surfacePresenter,
                new IndexPageFormContractTextResolver()),
            new IndexPageSelectedFormLoadErrorMapper());
        var formsEndpoint = new IndexPageFormsEndpoint(
            catalogLoader,
            new IndexPageFormsEndpointProjection(
                languageResolver,
                applicationOptionsResolver,
                selectedApplicationResolver,
                formOptionResolver));
        var workflow = new IndexPageWorkflow(
            catalogLoader,
            selectionResolver,
            selectedFormLoader,
            new IndexPageRequestReader());

        return new IndexPageRuntime(workflow, formsEndpoint);
    }

    private FormsPageLanguageResolver CreateLanguageResolver()
    {
        var languageCodeNormalizer = new FormsPageLanguageCodeNormalizer();
        var languageAvailabilityResolver = new FormsPageLanguageAvailabilityResolver(languageCodeNormalizer);

        return new FormsPageLanguageResolver(
            languageAvailabilityResolver,
            new FormsPageBrowserLanguageResolver(
                new FormsPageAcceptedLanguageHeaderResolver(languageAvailabilityResolver)),
            new FormsPageCatalogLanguageFallbackResolver(languageCodeNormalizer),
            languageCodeNormalizer);
    }
}
