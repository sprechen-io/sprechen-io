using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sprechen.Forms.Pages;
using Sprechen.Forms.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexPageFormsEndpointTests
{
    [Fact]
    public async Task GetFormsAsync_WhenCatalogLoadFails_ReturnsStableError()
    {
        var endpoint = CreateEndpoint(new DelegateFormsCatalogDataClient(_ =>
            throw new InvalidOperationException("Catalog failed.")));
        var context = new DefaultHttpContext();

        var result = await endpoint.GetFormsAsync(context.Request, "sample.checkout", "de", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var error = Assert.IsType<IndexPageFormsEndpointError>(jsonResult.Value);
        Assert.Equal(500, jsonResult.StatusCode);
        Assert.Equal("CATALOG_LOAD_FAILED", error.ErrorCode);
    }

    [Fact]
    public async Task GetFormsAsync_WhenApplicationSelected_ProjectsFormsForResolvedLanguage()
    {
        var endpoint = CreateEndpoint(new DelegateFormsCatalogDataClient(_ => Task.FromResult(CreateCatalog())));
        var context = new DefaultHttpContext();

        var result = await endpoint.GetFormsAsync(context.Request, "sample.checkout", "de", CancellationToken.None);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var response = Assert.IsType<IndexPageFormsEndpointResponse>(jsonResult.Value);
        Assert.Equal("sample.checkout", response.ApplicationId);
        Assert.Equal("de", response.Language);
        Assert.Collection(
            response.Forms,
            form =>
            {
                Assert.Equal("address-demo", form.FormId);
                Assert.Equal("Adresse", form.DisplayName);
            });
    }

    private IndexPageFormsEndpoint CreateEndpoint(IFormsCatalogDataClient catalogClient)
    {
        var languageCodeNormalizer = new FormsPageLanguageCodeNormalizer();
        var availabilityResolver = new FormsPageLanguageAvailabilityResolver(languageCodeNormalizer);
        var languageResolver = new FormsPageLanguageResolver(
            availabilityResolver,
            new FormsPageBrowserLanguageResolver(new FormsPageAcceptedLanguageHeaderResolver(availabilityResolver)),
            new FormsPageCatalogLanguageFallbackResolver(languageCodeNormalizer),
            languageCodeNormalizer);
        var applicationOptionsResolver = new IndexPageApplicationOptionsResolver();
        var selectedApplicationResolver = new IndexPageSelectedApplicationResolver();

        return new IndexPageFormsEndpoint(
            new FormsCatalogLoader(catalogClient),
            new IndexPageFormsEndpointProjection(
                languageResolver,
                applicationOptionsResolver,
                selectedApplicationResolver,
                new IndexPageFormOptionResolver()));
    }

    private FormsCatalog CreateCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("address-demo"),
                            DisplayName = new LocalizedText { ["de"] = "Adresse" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }
}
