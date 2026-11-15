using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureProfileCatalogReaderTests
{
    [Fact]
    public void Catalog_ContainsEveryEnumMode()
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetModeledModes();

        foreach (var mode in Enum.GetValues<SpeechCaptureMode>())
        {
            Assert.Contains(mode, modeled);
        }
    }

    [Fact]
    public void Catalog_ContainsOnlyKnownEnumModes()
    {
        var modeled = SpeechCaptureProfileCatalogReader.GetModeledModes();

        foreach (var mode in modeled)
        {
            Assert.True(Enum.IsDefined(mode), $"Catalog mode '{mode}' is not a known SpeechCaptureMode value.");
        }
    }

    [Theory]
    [InlineData(SpeechCaptureMode.ActivationListening, 4000)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation, 4000)]
    public void GetMaxUtteranceDuration_ForActivationModes_ReturnsModeledValue(SpeechCaptureMode mode, int expectedMs)
    {
        var actual = SpeechCaptureProfileCatalogReader.GetMaxUtteranceDuration(mode);

        Assert.NotNull(actual);
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), actual);
    }

    [Theory]
    [InlineData(SpeechCaptureMode.ActivationListening, 8000)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation, 8000)]
    public void GetMaxTotalSegmentDuration_ForActivationModes_ReturnsModeledValue(SpeechCaptureMode mode, int expectedMs)
    {
        var actual = SpeechCaptureProfileCatalogReader.GetMaxTotalSegmentDuration(mode);

        Assert.NotNull(actual);
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), actual);
    }

    [Fact]
    public void GetMaxTotalSegmentDuration_ForUnwiredNonActivationMode_ReturnsNull()
    {
        // Für Listening gibt der Katalog hier keine eigene Gesamtgrenze vor.
        Assert.Null(SpeechCaptureProfileCatalogReader.GetMaxTotalSegmentDuration(SpeechCaptureMode.Listening));
    }

    [Theory]
    [InlineData(SpeechCaptureMode.Listening)]
    [InlineData(SpeechCaptureMode.ListeningContinuation)]
    [InlineData(SpeechCaptureMode.PromptInterruption)]
    [InlineData(SpeechCaptureMode.PromptInterruptionDetection)]
    [InlineData(SpeechCaptureMode.ShortConfirmation)]
    public void GetMaxUtteranceDuration_ForUnwiredNonActivationMode_ReturnsNull(SpeechCaptureMode mode)
    {
        Assert.Null(SpeechCaptureProfileCatalogReader.GetMaxUtteranceDuration(mode));
    }

    [Theory]
    [InlineData(SpeechCaptureMode.ActivationListening, 200)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation, 200)]
    public void GetSpeechStartConfirmation_ForActivationModes_ReturnsModeledValue(SpeechCaptureMode mode, int expectedMs)
    {
        var actual = SpeechCaptureProfileCatalogReader.GetSpeechStartConfirmation(mode);

        Assert.NotNull(actual);
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), actual);
    }

    [Fact]
    public void GetSilenceDebounceMs_ForListeningMode_ReturnsModeledDefault()
    {
        Assert.Equal(500, SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.Listening));
    }

    [Fact]
    public void GetOptionListingInitialSilenceTimeoutMs_ReturnsModeledValue()
    {
        Assert.Equal(18000, SpeechCaptureProfileCatalogReader.GetOptionListingInitialSilenceTimeoutMs());
    }
}
