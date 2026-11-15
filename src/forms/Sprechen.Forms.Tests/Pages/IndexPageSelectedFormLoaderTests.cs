using Sprechen.Forms.Pages;
using Sprechen.Forms.Pages.Models;
using Sprechen.Forms.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Web.FormSurfaces;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexPageSelectedFormLoaderTests
{
    [Fact]
    public async Task LoadAsync_WhenHtmlProjectionProfileUnsupported_ReturnsStableError()
    {
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
            Projections = new ContractProjections
            {
                Html = new HtmlProjection { Profile = "highContrast" }
            }
        };
        var loader = new IndexPageSelectedFormLoader(
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new IndexPageSelectedFormProjectionBuilder(
                new DelegateReferenceFormFieldBuilder((_, _, _) =>
                    throw new InvalidOperationException("Should not build unsupported projection.")),
                new FormSurfacePresenter(),
                new IndexPageFormContractTextResolver()),
            new IndexPageSelectedFormLoadErrorMapper());

        var result = await loader.LoadAsync(
            new IndexPageSelectedFormRequest(
                CreateApplication(),
                new FormOption("address-demo", "Adresse", "1", 1),
                "sample.checkout",
                "de"),
            CancellationToken.None);

        Assert.Equal(HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile, result.ErrorCode);
        Assert.Null(result.Surface);
    }

    private ApplicationDescriptor CreateApplication()
    {
        return new ApplicationDescriptor
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
        };
    }
}
