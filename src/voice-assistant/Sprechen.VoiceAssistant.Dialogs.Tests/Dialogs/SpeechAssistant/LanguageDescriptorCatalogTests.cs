namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die unterstützten Sprachkennungen, die Behandlung unbekannter Kennungen
/// und die Zuordnung von Kultur und zeitbezogener Sprachauswertung.
/// </summary>
public sealed class LanguageDescriptorCatalogTests
{
    [Theory]
    [InlineData("de", "de-DE", false)]
    [InlineData("en", "en-US", false)]
    [InlineData("fr", "fr-FR", true)]
    [InlineData("es", "es-ES", true)]
    [InlineData("it", "it-IT", true)]
    public void For_ResolvesEverySupportedLanguage(
        string isoCode,
        string expectedCulture,
        bool expectedRomance)
    {
        var descriptor = LanguageDescriptorCatalog.For(isoCode);

        Assert.Equal(expectedCulture, descriptor.CultureName);
        Assert.Equal(expectedRomance, descriptor.UsesRomanceTemporalParsing);
    }

    [Fact]
    public void For_ThrowsOnUnknownCode_InsteadOfSilentlyDefaulting()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LanguageDescriptorCatalog.For("pt"));

        Assert.Contains("pt", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void For_ThrowsOnEmptyCode()
    {
        Assert.Throws<ArgumentException>(() => LanguageDescriptorCatalog.For(" "));
    }

    [Fact]
    public void TryGet_NormalizesRegionAndScriptSuffixes()
    {
        Assert.True(LanguageDescriptorCatalog.TryGet("de-DE", out var descriptor));
        Assert.Equal("de", descriptor.IsoCode);
        Assert.True(LanguageDescriptorCatalog.TryGet("de_DE", out descriptor));
        Assert.Equal("de", descriptor.IsoCode);
    }

    [Fact]
    public void TryGet_WhenCodeIsNull_ReturnsFalse()
    {
        Assert.False(LanguageDescriptorCatalog.TryGet(null, out _));
    }

    [Fact]
    public void Validate_PassesForTheCompleteCatalog()
    {
        // Die explizite Prüfung soll wie die Prüfung beim Initialisieren ohne Ausnahme enden.
        LanguageDescriptorCatalog.Validate();
    }

    [Fact]
    public void Catalog_CoversExactlyTheSupportedKeySet()
    {
        foreach (var isoCode in LanguageDescriptors.AllIsoCodes)
        {
            var descriptor = LanguageDescriptorCatalog.For(isoCode);
            Assert.Equal(isoCode, descriptor.IsoCode);
        }
    }
}
