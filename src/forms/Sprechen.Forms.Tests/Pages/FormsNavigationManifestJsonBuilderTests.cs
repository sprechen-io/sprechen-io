using System.Text.Json;
using Sprechen.Forms.Pages;
using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class FormsNavigationManifestJsonBuilderTests
{
    [Fact]
    public void Build_PreservesCamelCaseJsonAndTrimsNavigationMetadata()
    {
        var builder = new FormsNavigationManifestJsonBuilder(new FormsNavigationManifestProjector());

        var json = builder.Build(
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
                        AvailableContractVersions = ["1"],
                        PrerequisiteForms = [" prerequisite-form ", ""],
                        SharedVariables = [" customerId ", " "]
                    }
                ]
            },
            [new FormOption("address-demo", "Adresse", "1", 1)]);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var form = root.GetProperty("forms")[0];

        Assert.Equal("sample.checkout", root.GetProperty("applicationId").GetString());
        Assert.Equal("address-demo", form.GetProperty("formId").GetString());
        Assert.Equal("prerequisite-form", form.GetProperty("prerequisiteForms")[0].GetString());
        Assert.Equal("customerId", form.GetProperty("sharedVariables")[0].GetString());
    }
}
