using Sprechen.Forms.Pages;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexPageSelectionResolverTests
{
    [Fact]
    public void Resolve_WhenSingleFormApplicationIsSelected_AutoSelectsFormAndBuildsNavigation()
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            CreateCatalogWithSingleForm(),
            new IndexPageSelectionRequest(null, "de", "sample.checkout", null, null));

        Assert.Equal("sample.checkout", result.SelectedApplicationId);
        Assert.Equal("address-demo", result.SelectedFormId);
        Assert.NotNull(result.SelectedApplication);
        Assert.NotNull(result.SelectedForm);
        Assert.Equal(1, result.CurrentFormIndex);
        Assert.Equal(1, result.TotalFormsCount);
        Assert.Contains("address-demo", result.NavigationManifestJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WhenApplicationChanged_DropsPreviousFormSelection()
    {
        var resolver = CreateResolver();

        var result = resolver.Resolve(
            CreateCatalogWithMultipleForms(),
            new IndexPageSelectionRequest(null, "de", "sample.multi", "form1", "sample.checkout"));

        Assert.Equal("sample.multi", result.SelectedApplicationId);
        Assert.Equal(string.Empty, result.SelectedFormId);
        Assert.Null(result.SelectedForm);
        Assert.Equal(2, result.Forms.Count);
        Assert.Equal(0, result.CurrentFormIndex);
    }

    private FormsCatalog CreateCatalogWithSingleForm()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                CreateApplication(
                    "sample.checkout",
                    [
                        CreateForm("address-demo", "Adresse")
                    ])
            ]
        };
    }

    private FormsCatalog CreateCatalogWithMultipleForms()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                CreateApplication(
                    "sample.multi",
                    [
                        CreateForm("form1", "Form 1"),
                        CreateForm("form2", "Form 2")
                    ])
            ]
        };
    }

    private ApplicationDescriptor CreateApplication(string applicationId, List<FormDescriptor> forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = applicationId,
            DisplayName = new LocalizedText { ["de"] = applicationId },
            Description = new LocalizedText { ["de"] = "Demo" },
            Tags = ["sample"],
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = forms
        };
    }

    private FormDescriptor CreateForm(string formId, string displayName)
    {
        return new FormDescriptor
        {
            FormId = FormId.From(formId),
            DisplayName = new LocalizedText { ["de"] = displayName },
            Description = new LocalizedText { ["de"] = "Demo" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };
    }

    private IndexPageSelectionResolver CreateResolver()
    {
        var languageCodeNormalizer = new FormsPageLanguageCodeNormalizer();
        var languageAvailabilityResolver = new FormsPageLanguageAvailabilityResolver(languageCodeNormalizer);

        return new IndexPageSelectionResolver(
            new FormsPageLanguageResolver(
                languageAvailabilityResolver,
                new FormsPageBrowserLanguageResolver(new FormsPageAcceptedLanguageHeaderResolver(languageAvailabilityResolver)),
                new FormsPageCatalogLanguageFallbackResolver(languageCodeNormalizer),
                languageCodeNormalizer),
            new IndexPageApplicationOptionsResolver(),
            new IndexPageSelectedApplicationResolver(),
            new IndexPageFormOptionResolver(),
            new IndexPageSelectedFormResolver(),
            new FormsNavigationManifestJsonBuilder(new FormsNavigationManifestProjector()));
    }
}
