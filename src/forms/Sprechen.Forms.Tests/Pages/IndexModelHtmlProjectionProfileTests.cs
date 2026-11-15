using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprechen.Forms.Pages;
using Sprechen.Forms.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Web.FormSurfaces;
using Xunit;
using ReferenceFormDefinition = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceDefinition;
using ReferenceFormField = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceField;
using ReferenceFormFieldGroupInfo = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceFieldGroupInfo;
using ReferenceFormSurfacePresenter = Sprechen.Web.FormSurfaces.FormSurfacePresenter;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexModelHtmlProjectionProfileTests
{
    [Fact]
    public async Task OnGetAsync_ContractWithoutProjections_DefaultsToA11yFirst()
    {
        var (catalog, contract) = CreateCatalogAndContract(htmlProjectionProfile: null);

        var model = BuildModel(catalog, contract);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Null(model.ErrorCode);
        Assert.Equal(HtmlProjectionProfiles.A11yFirst, model.HtmlProjectionProfile);
    }

    [Fact]
    public async Task OnGetAsync_ContractWithA11yFirstProfile_ResolvesA11yFirst()
    {
        var (catalog, contract) = CreateCatalogAndContract(htmlProjectionProfile: HtmlProjectionProfiles.A11yFirst);

        var model = BuildModel(catalog, contract);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Null(model.ErrorCode);
        Assert.Equal(HtmlProjectionProfiles.A11yFirst, model.HtmlProjectionProfile);
    }

    [Fact]
    public async Task OnGetAsync_ContractWithUnsupportedProfile_SurfacesStableErrorCode()
    {
        var (catalog, contract) = CreateCatalogAndContract(htmlProjectionProfile: "highContrast");

        var model = BuildModel(catalog, contract);

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile, model.ErrorCode);
        // Hier wird nur der Fehlercode bereitgestellt; den lokalisierten Text liefert
        // der Textkatalog beim Rendern.
        Assert.Null(model.ErrorMessage);
    }

    private static IndexModel BuildModel(FormsCatalog catalog, FormContract contract)
    {
        return new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => new ReferenceFormDefinition(
                entryFieldGroupId: "entry",
                fieldGroupsById: new Dictionary<string, ReferenceFormFieldGroupInfo>(StringComparer.OrdinalIgnoreCase),
                fields: Array.Empty<ReferenceFormField>())),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout",
            FormId = "address-demo"
        };
    }

    private static (FormsCatalog Catalog, FormContract Contract) CreateCatalogAndContract(string? htmlProjectionProfile)
    {
        var catalog = new FormsCatalog
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

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.checkout",
            FormId = FormId.From("address-demo"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false,
            Projections = htmlProjectionProfile is null
                ? null
                : new ContractProjections { Html = new HtmlProjection { Profile = htmlProjectionProfile } }
        };

        return (catalog, contract);
    }
}
