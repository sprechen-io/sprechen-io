using Sprechen.Forms.Pages;
using Sprechen.Forms.Pages.Models;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexPageSelectedFormResolverTests
{
    [Fact]
    public void Resolve_WhenApplicationChanged_DropsRequestedFormSelection()
    {
        var resolver = new IndexPageSelectedFormResolver();

        var result = resolver.Resolve(
            new IndexPageSelectionRequest(null, "de", "sample.multi", "form1", "sample.checkout"),
            "sample.multi",
            [
                new FormOption("form1", "Form 1", "1", 1),
                new FormOption("form2", "Form 2", "1", 2)
            ]);

        Assert.Equal(string.Empty, result.SelectedFormId);
        Assert.Null(result.SelectedForm);
        Assert.Equal(0, result.CurrentFormIndex);
    }

    [Fact]
    public void Resolve_WhenUnknownFormInSingleFormApplication_AutoSelectsSingleForm()
    {
        var resolver = new IndexPageSelectedFormResolver();

        var result = resolver.Resolve(
            new IndexPageSelectionRequest(null, "de", "sample.checkout", "unknown", "sample.checkout"),
            "sample.checkout",
            [new FormOption("address-demo", "Adresse", "1", 1)]);

        Assert.Equal("address-demo", result.SelectedFormId);
        Assert.NotNull(result.SelectedForm);
        Assert.Equal(1, result.CurrentFormIndex);
    }
}
