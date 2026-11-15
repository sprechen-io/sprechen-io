namespace Sprechen.Forms.Tests.Pages;


public sealed class FormsChromeCopyTests
{
    [Fact]
    public void Entries_ResolveEveryCatalogIdWithAllLanguages()
    {
        var entries = Sprechen.Forms.Pages.FormsChromeCopy.Entries;

        Assert.NotEmpty(entries);
        Assert.Equal(
            Sprechen.Mda.Contracts.V1.Catalog.FormsChromeCopyLoader.Catalog.Entries.Count,
            entries.Count);

        foreach (var (copyId, entry) in entries)
        {
            foreach (var language in new[] { "de", "en", "fr", "es", "it" })
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(entry.For(language)),
                    $"Chrome copy '{copyId}' has no text for '{language}'.");
            }
        }
    }

    [Theory]
    [InlineData("NavSubmit", "de", "Absenden")]
    [InlineData("NavSubmit", "en", "Submit")]
    [InlineData("ActionLoad", "de", "Weiter")]
    [InlineData("LabelLanguage", "de", "Sprache")]
    [InlineData("ValidationRequiredFallback", "en", "This field is required.")]
    [InlineData("ErrorCatalogLoadFailed", "de", "Der Anwendungskatalog konnte nicht geladen werden.")]
    [InlineData("ErrorFormLocalizationStrictMissing", "de", "Das Formular enthält unvollständige Lokalisierungen für eine oder mehrere Sprachen.")]
    [InlineData("ErrorFormPreflightFailed", "de", "Das Formular ist ungültig und konnte nicht geladen werden.")]
    [InlineData("ErrorFormProjectionProfileUnsupported", "de", "Das Formular nutzt ein nicht unterstütztes HTML-Projektion-Profil.")]
    [InlineData("ErrorFormLoadFailed", "de", "Das Formular konnte nicht geladen werden.")]
    [InlineData("CancelConfirm", "de", "Sind Sie sicher, dass Sie abbrechen m\u00f6chten? Ihre Eingaben gehen verloren.")]
    [InlineData("CancelTitle", "fr", "Annul\u00e9")]
    [InlineData("CancelledText", "es", "La sesi\u00f3n del formulario se ha cancelado.")]
    [InlineData("SuccessNextSteps", "it", "Ora pu\u00f2 chiudere questa finestra o tornare alla selezione.")]
    [InlineData("SuccessSubmittedText", "fr", "Le formulaire a \u00e9t\u00e9 envoy\u00e9 avec succ\u00e8s.")]
    public void Select_KnownIds_KeepTheirCanonicalTexts(string copyId, string language, string expected)
    {
        Assert.Equal(expected, Sprechen.Forms.Pages.FormsChromeCopy.Select(copyId, language));
    }

    [Fact]
    public void Select_UnknownLanguage_FallsBackToGerman()
    {
        Assert.Equal(
            Sprechen.Forms.Pages.FormsChromeCopy.Select("NavSubmit", "de"),
            Sprechen.Forms.Pages.FormsChromeCopy.Select("NavSubmit", "pt"));
    }

    [Fact]
    public void Loader_RejectsAnEntryWithAMissingLanguageText()
    {
        var catalog = new Sprechen.Mda.Contracts.V1.Catalog.FormsChromeCopyCatalog
        {
            CatalogId = "test",
            CatalogVersion = "1",
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            Entries =
            [
                new Sprechen.Mda.Contracts.V1.Catalog.FormsChromeCopyEntry
                {
                    Id = "Broken",
                    Texts = new Sprechen.Mda.Contracts.V1.LocalizedText { ["de"] = "nur deutsch" }
                }
            ]
        };

        Assert.Throws<InvalidOperationException>(() => Sprechen.Mda.Contracts.V1.Catalog.FormsChromeCopyLoader.Validate(catalog));
    }
}
