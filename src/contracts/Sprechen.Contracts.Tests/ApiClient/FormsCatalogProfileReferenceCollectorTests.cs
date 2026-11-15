using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class FormsCatalogProfileReferenceCollectorTests
{
    [Fact]
    public void Collect_ReturnsDistinctCatalogAndApplicationProfileReferences()
    {
        var collector = new FormsCatalogProfileReferenceCollector();
        var document = new FormsCatalogDocument
        {
            SelectionDialogProfile = new ApplicationSelectionDialogProfileReference
            {
                ProfileId = "global",
                ProfileVersion = "1"
            },
            Applications =
            [
                new ApplicationDescriptorDocument
                {
                    SelectionDialogProfile = new ApplicationSelectionDialogProfileReference
                    {
                        ProfileId = "global",
                        ProfileVersion = "1"
                    }
                },
                new ApplicationDescriptorDocument
                {
                    SelectionDialogProfile = new ApplicationSelectionDialogProfileReference
                    {
                        ProfileId = "application",
                        ProfileVersion = "2"
                    }
                }
            ]
        };

        var references = collector.Collect(document);

        Assert.Equal(2, references.Count);
        Assert.Contains(new ApplicationSelectionDialogProfileKey("global", "1"), references);
        Assert.Contains(new ApplicationSelectionDialogProfileKey("application", "2"), references);
    }
}
