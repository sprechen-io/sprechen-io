using Sprechen.Service.Deepgram;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests;

// Änderungen der voreingestellten TTS-Stimmen erfordern eine erneute Prüfung des Lautstärkeausgleichs beim Client.
public sealed class DeepgramTtsVoiceDefaultsTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedTtsModels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["de"] = "aura-2-aurelia-de",
            ["en"] = "aura-2-iris-en",
            ["fr"] = "aura-2-agathe-fr",
            ["es"] = "aura-2-silvia-es",
            ["it"] = "aura-2-maia-it",
        };

    [Fact]
    public void GermanDefaultVoice_IsAurelia()
    {
        var options = new DeepgramOptions();

        Assert.Equal("aura-2-aurelia-de", options.TtsModels["de"]);
    }

    [Fact]
    public void TtsModelDefaults_MatchTheConfiguredVoices()
    {
        var options = new DeepgramOptions();

        foreach (var (iso, expectedVoice) in ExpectedTtsModels)
        {
            Assert.True(options.TtsModels.TryGetValue(iso, out var voice), $"missing TTS model for {iso}");
            Assert.Equal(expectedVoice, voice);
        }

        Assert.Equal(ExpectedTtsModels.Count, options.TtsModels.Count);
    }
}
