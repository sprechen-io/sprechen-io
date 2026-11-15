using System.Reflection;
using Sprechen.Forms.Pages;

namespace Sprechen.Forms.Tests.Pages;

public sealed class FormsChromeCopyResolverTests
{

    private static readonly string[] SupportedLanguages = ["de", "en", "fr", "es", "it"];

    private static IReadOnlyList<PropertyInfo> StringChromeKeys()
        => typeof(FormsChromeCopyResolver)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(string))
            .ToArray();

    [Fact]
    public void EveryChromeKey_ResolvesNonEmpty_ForEverySupportedLanguage()
    {
        var keys = StringChromeKeys();
        Assert.NotEmpty(keys);

        var blanks = new List<string>();
        foreach (var language in SupportedLanguages)
        {
            var resolver = FormsChromeCopyResolver.For(language);
            foreach (var key in keys)
            {
                var value = (string?)key.GetValue(resolver);
                if (string.IsNullOrWhiteSpace(value))
                {
                    blanks.Add($"{key.Name} [{language}]");
                }
            }
        }

        Assert.True(
            blanks.Count == 0,
            "Every Forms chrome key must resolve to a non-empty string for every supported display "
            + "language. Blank/missing: " + string.Join(", ", blanks));
    }

    [Theory]
    [InlineData("de", "Kein Inhalt")]
    [InlineData("en", "No content")]
    [InlineData("fr", "Aucun contenu")]
    [InlineData("es", "Sin contenido")]
    [InlineData("it", "Nessun contenuto")]
    public void ReadOnlyEmptyValue_MirrorsTheVoiceEmptyValueWordingFamily(string language, string expected)
    {
        // Der Text soll inhaltlich zur Sprachausgabe unter dialog.field.readOnly.emptyValue passen.


        Assert.Equal(expected, FormsChromeCopyResolver.For(language).ReadOnlyEmptyValue);
    }

    [Fact]
    public void NavStep_ResolvesNonEmptyAndEmbedsTheNumbers_ForEverySupportedLanguage()
    {
        foreach (var language in SupportedLanguages)
        {
            var resolver = FormsChromeCopyResolver.For(language);
            var value = resolver.NavStep(2, 5);

            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.Contains("2", value, StringComparison.Ordinal);
            Assert.Contains("5", value, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("de", "Navigationsebene Formular: Formular 2 von 5")]
    [InlineData("en", "Navigation level form: form 2 of 5")]
    [InlineData("fr", "Niveau de navigation formulaire : formulaire 2 sur 5")]
    public void NavStep_FormatsTheFormNumbers_ByteFaithfully(string language, string expected)
    {
        var resolver = FormsChromeCopyResolver.For(language);

        Assert.Equal(expected, resolver.NavStep(2, 5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("pl")]
    [InlineData("zz")]
    public void For_UnsupportedOrEmptyLanguage_FallsBackToGerman(string? language)
    {
        var resolver = FormsChromeCopyResolver.For(language);


        Assert.Equal("Zurueck", resolver.NavBack);
        Assert.Equal("Einstellungen", resolver.Settings);
    }

    [Theory]

    [InlineData("de", "Zurück zu den Formularen")]
    [InlineData("fr", "Retour aux formulaires")]
    public void BackLabel_PinsTheMigratedCopy(string language, string expected)
    {
        Assert.Equal(expected, FormsChromeCopyResolver.For(language).BackLabel);
    }

    [Theory]
    [InlineData("de", "Einstellungen")]
    [InlineData("en", "Settings")]
    [InlineData("fr", "Paramètres")]
    [InlineData("es", "Ajustes")]
    [InlineData("it", "Impostazioni")]
    public void Settings_IsSharedAndLocalized_AcrossLanguages(string language, string expected)
    {
        Assert.Equal(expected, FormsChromeCopyResolver.For(language).Settings);
    }

    [Theory]

    [InlineData("CATALOG_LOAD_FAILED", "Der Anwendungskatalog konnte nicht geladen werden.")]
    [InlineData("E_LOCALIZATION_STRICT_MISSING", "Das Formular enthält unvollständige Lokalisierungen für eine oder mehrere Sprachen.")]
    [InlineData("FORM_CONTRACT_PREFLIGHT_FAILED", "Das Formular ist ungültig und konnte nicht geladen werden.")]
    [InlineData("E_HTML_PROJECTION_PROFILE_UNSUPPORTED", "Das Formular nutzt ein nicht unterstütztes HTML-Projektion-Profil.")]
    [InlineData("FORM_CONTRACT_LOAD_FAILED", "Das Formular konnte nicht geladen werden.")]
    public void ErrorMessageFor_KnownCodes_KeepsTheHistoricalGermanTexts(string errorCode, string expected)
    {
        Assert.Equal(expected, FormsChromeCopyResolver.For("de").ErrorMessageFor(errorCode));
    }

    [Theory]
    [InlineData("CATALOG_LOAD_FAILED", "en", "The application catalog could not be loaded.")]
    [InlineData("CATALOG_LOAD_FAILED", "fr", "Le catalogue d'applications n'a pas pu être chargé.")]
    [InlineData("CATALOG_LOAD_FAILED", "es", "El catálogo de aplicaciones no se pudo cargar.")]
    [InlineData("CATALOG_LOAD_FAILED", "it", "Il catalogo delle applicazioni non ha potuto essere caricato.")]
    [InlineData("FORM_CONTRACT_LOAD_FAILED", "en", "The form could not be loaded.")]
    [InlineData("E_HTML_PROJECTION_PROFILE_UNSUPPORTED", "en", "The form uses an unsupported HTML projection profile.")]
    public void ErrorMessageFor_KnownCodes_ResolvesTheSessionLanguage(string errorCode, string language, string expected)
    {
        Assert.Equal(expected, FormsChromeCopyResolver.For(language).ErrorMessageFor(errorCode));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("SOME_FUTURE_CODE")]
    public void ErrorMessageFor_UnknownCode_ReturnsNullForRawFallback(string? errorCode)
    {

        Assert.Null(FormsChromeCopyResolver.For("de").ErrorMessageFor(errorCode));
    }
}
