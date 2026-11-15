using System.Text.Json;
using Xunit;
using Sprechen.Service.Core.Services;

namespace Sprechen.Service.Core.Tests;

/// <summary>
/// Prüft den Rückfall auf den Sprachcode bei unbekannten Sprachen.
/// </summary>
public sealed class LanguageCatalogModelAlignmentTests
{

    [Fact]
    public void UnsupportedLanguage_FallsBackToTheIsoCode()
    {
        var catalog = new LanguageCatalog();
        Assert.Equal("pt", catalog.GetEndonym(LanguageCode.ParseIso6391("pt")));
    }
}
