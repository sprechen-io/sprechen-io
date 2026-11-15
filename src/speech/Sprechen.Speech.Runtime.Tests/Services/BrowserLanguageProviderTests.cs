using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BrowserLanguageProviderTests
{
    [Fact]
    public async Task GetPreferredLanguageTagsAsync_WhenJsInteropReturnsNavigatorLanguages_ReturnsNormalizedBrowserTags()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("userLanguageSettings.getPreferredLanguageTags", new[] { "fr-CA", "en-US", "fr-CA " });

        var provider = new BrowserLanguageProvider(jsRuntime, NullLogger<BrowserLanguageProvider>.Instance);

        var languageTags = await provider.GetPreferredLanguageTagsAsync(CancellationToken.None);

        Assert.Equal(["fr-CA", "en-US"], languageTags);
    }

    [Fact]
    public async Task GetPreferredLanguageTagsAsync_WhenJsInteropReturnsNoLanguages_FallsBackToCurrentCultureTags()
    {
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = new CultureInfo("it-IT");
            var jsRuntime = new FakeJsRuntime();
            jsRuntime.SetResult("userLanguageSettings.getPreferredLanguageTags", Array.Empty<string>());

            var provider = new BrowserLanguageProvider(jsRuntime, NullLogger<BrowserLanguageProvider>.Instance);

            var languageTags = await provider.GetPreferredLanguageTagsAsync(CancellationToken.None);

            Assert.Equal(["de-DE", "it-IT"], languageTags);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task GetPreferredLanguageTagsAsync_WhenJsInteropThrowsAndNoCultureTagsExist_ReturnsEmpty()
    {
        var originalUiCulture = CultureInfo.CurrentUICulture;
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var jsRuntime = new FakeJsRuntime();
            jsRuntime.SetException("userLanguageSettings.getPreferredLanguageTags", new JSException("boom"));

            var provider = new BrowserLanguageProvider(jsRuntime, NullLogger<BrowserLanguageProvider>.Instance);

            var languageTags = await provider.GetPreferredLanguageTagsAsync(CancellationToken.None);

            Assert.Empty(languageTags);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalUiCulture;
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
