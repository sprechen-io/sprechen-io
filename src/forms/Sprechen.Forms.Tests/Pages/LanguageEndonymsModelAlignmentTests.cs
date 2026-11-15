using System.Text.Json;

namespace Sprechen.Forms.Tests.Pages;

/// <summary>
/// Prüft die Rückgabe unbekannter Sprachcodes.
/// </summary>
public sealed class LanguageEndonymsModelAlignmentTests
{

    [Fact]
    public void UnknownCode_FallsBackToTheRawCode()
    {
        Assert.Equal("pt", Sprechen.Forms.Pages.LanguageEndonyms.For("pt"));
    }
}
