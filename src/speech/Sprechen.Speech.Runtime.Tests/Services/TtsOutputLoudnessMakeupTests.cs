using Sprechen.Speech.App.GrpcInfrastructure.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

// Prüft Sprach- und Stimmzuordnung sowie die angewandten Lautstärkefaktoren des TTS-Katalogs.
public sealed class TtsOutputLoudnessMakeupTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedVoices =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["de"] = "aura-2-aurelia-de",
            ["en"] = "aura-2-iris-en",
            ["fr"] = "aura-2-agathe-fr",
            ["es"] = "aura-2-silvia-es",
            ["it"] = "aura-2-maia-it",
        };

    [Fact]
    public void Catalog_HasExactlyTheFiveSupportedLanguages()
    {
        var catalog = new TtsOutputLoudnessMakeup();

        var isos = catalog.Entries.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "de", "en", "es", "fr", "it" }, isos);
    }

    [Fact]
    public void Catalog_EachEntryRecordsItsMeasuredVoiceMatchingTheConfiguredAuraVoice()
    {
        var catalog = new TtsOutputLoudnessMakeup();

        foreach (var (iso, expectedVoice) in ExpectedVoices)
        {
            Assert.True(catalog.Entries.TryGetValue(iso, out var entry), $"missing catalog entry for {iso}");
            Assert.Equal(expectedVoice, entry.MeasuredVoice);
        }
    }

    [Fact]
    public void Catalog_MakeupValuesAreFiniteAndWithinASaneBoostRange()
    {
        var catalog = new TtsOutputLoudnessMakeup();

        foreach (var (iso, entry) in catalog.Entries)
        {
            Assert.True(double.IsFinite(entry.MakeupDb), $"{iso} makeup must be finite");
            // Die hinterlegten Werte dürfen größer als die separat geprüfte Grenze der Anwendung sein.
            Assert.InRange(entry.MakeupDb, 0.0, 16.0);
        }
    }

    [Theory]
    [InlineData("de", 9.0)]   // Auf die angewandte Obergrenze begrenzt.
    [InlineData("en", 2.2)]
    [InlineData("fr", 8.0)]
    [InlineData("es", 7.1)]
    [InlineData("it", 8.5)]
    public void GetFactor_ReturnsTheLinearMakeupForTheLanguage(string languageCode, double expectedDb)
    {
        var catalog = new TtsOutputLoudnessMakeup();

        var factor = catalog.GetFactor(languageCode);

        Assert.Equal(Math.Pow(10, expectedDb / 20.0), factor, precision: 6);
    }

    // Der hinterlegte Messwert bleibt erhalten, auch wenn bei der Wiedergabe weniger Verstärkung angewandt wird.
    [Fact]
    public void AppliedMakeup_IsBounded_EvenWhenTheMeasuredValueIsHigher()
    {
        var catalog = new TtsOutputLoudnessMakeup();

        foreach (var (iso, entry) in catalog.Entries)
        {
            var applied = TtsOutputLoudnessMakeup.GetAppliedMakeupDb(entry);

            Assert.True(applied <= 9.0, $"{iso} applies {applied} dB, above the bound");
            Assert.True(applied <= entry.MakeupDb, $"{iso} applies more than it measured");
        }

        var german = catalog.Entries["de"];
        Assert.True(german.MakeupDb > 9.0, "the German entry must keep its measured value, not the bounded one");
        Assert.Equal(9.0, TtsOutputLoudnessMakeup.GetAppliedMakeupDb(german));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("DE")]
    [InlineData("de-AT")]
    public void GetFactor_NormalizesRegionalAndCasedCodesToThePrimaryLanguage(string languageCode)
    {
        var catalog = new TtsOutputLoudnessMakeup();

        Assert.Equal(catalog.GetFactor("de"), catalog.GetFactor(languageCode));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zz")]
    [InlineData("xx-YY")]
    public void GetFactor_FallsBackToUnity_ForUnknownOrEmptyLanguage(string? languageCode)
    {
        var catalog = new TtsOutputLoudnessMakeup();

        Assert.Equal(1.0, catalog.GetFactor(languageCode));
    }
}
