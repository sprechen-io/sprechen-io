using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormsCatalogOwnerTests
{
    [Fact]
    public void GetSupportedLanguages_ReturnsDistinctSortedTrimmedLanguages()
    {
        var catalog = new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    SupportedLanguages = [" en ", "de"]
                },
                new ApplicationDescriptor
                {
                    SupportedLanguages = ["fr", "de", "EN"]
                }
            ]
        };

        var result = new FormsCatalogLanguageIndex().GetSupportedLanguages(catalog);

        Assert.Equal(["de", "en", "fr"], result);
    }

    [Fact]
    public void IsLanguageSupported_NormalizesRegionTag()
    {
        var application = new ApplicationDescriptor
        {
            SupportedLanguages = ["de", "en"]
        };

        var result = new FormsCatalogLanguageIndex().IsLanguageSupported(application, "de-CH");

        Assert.True(result);
    }

    [Fact]
    public void ResolveApplicationDisplayName_UsesApplicationPolitenessByDefault()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "tax-app",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            VoiceDialogSettings = new VoiceDialogSettings
            {
                PolitenessLevel = PolitenessLevel.Informal
            },
            DisplayName = new LocalizedText
            {
                ["de"] = "Steueranwendung",
                ["de@informal"] = "Steuerapp"
            }
        };

        var result = new FormsCatalogTextResolver().ResolveApplicationDisplayName(application, "de");

        Assert.Equal("Steuerapp", result);
    }

    [Fact]
    public void ResolveApplicationDescription_ReturnsNull_WhenDescriptionCannotBeResolved()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "tax-app",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de"
        };

        var result = new FormsCatalogTextResolver().ResolveApplicationDescription(application, "de");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveCatalogText_UsesFallback_WhenLocalizedTextIsMissing()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "tax-app",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de"
        };

        var result = new FormsCatalogTextResolver().ResolveCatalogText(application, null, "de", "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ResolveFormDisplayName_UsesExplicitPolitenessLevel()
    {
        var application = new ApplicationDescriptor
        {
            SupportedLanguages = ["de"],
            DefaultLanguage = "de"
        };
        var form = new FormDescriptor
        {
            FormId = FormId.From("profile"),
            DisplayName = new LocalizedText
            {
                ["de"] = "Profil",
                ["de@informal"] = "Dein Profil"
            }
        };

        var result = new FormsCatalogTextResolver().ResolveFormDisplayName(
            application,
            form,
            "de",
            PolitenessLevel.Informal);

        Assert.Equal("Dein Profil", result);
    }

    [Fact]
    public void OrderForms_SortsBySequenceThenFormId_AndSkipsEmptyIds()
    {
        var application = new ApplicationDescriptor
        {
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("zeta"), Sequence = null },
                new FormDescriptor { FormId = FormId.From("beta"), Sequence = 2 },
                new FormDescriptor { FormId = FormId.From("alpha"), Sequence = 2 },
                new FormDescriptor { FormId = FormId.From("gamma"), Sequence = 1 },
                new FormDescriptor { FormId = default }
            ]
        };

        var result = new FormsCatalogFormOrder().OrderForms(application);

        Assert.Equal(["gamma", "alpha", "beta", "zeta"], result.Select(form => form.FormId.Value));
    }

    [Fact]
    public void GetDefaultFormId_ReturnsFirstOrderedFormId()
    {
        var application = new ApplicationDescriptor
        {
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("later"), Sequence = 2 },
                new FormDescriptor { FormId = FormId.From("first"), Sequence = 1 }
            ]
        };

        var result = new FormsCatalogFormOrder().GetDefaultFormId(application);

        Assert.Equal("first", result);
    }
}
