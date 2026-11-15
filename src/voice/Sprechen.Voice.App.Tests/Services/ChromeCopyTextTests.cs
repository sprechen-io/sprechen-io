namespace Sprechen.Voice.App.Tests.Services;


public sealed class ChromeCopyTextTests
{
    private static readonly ChromeCopyText Entry = new("de-text", "en-text", "fr-text", "es-text", "it-text");

    [Theory]
    [InlineData("de", "de-text")]
    [InlineData("en", "en-text")]
    [InlineData("fr", "fr-text")]
    [InlineData("es", "es-text")]
    [InlineData("it", "it-text")]
    [InlineData("pt", "de-text")]
    [InlineData(null, "de-text")]
    public void For_SelectsTheLanguage_WithGermanFallback(string? language, string expected)
        => Assert.Equal(expected, Entry.For(language));
}
