using System.Linq;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Vergleicht die modellierten Segmenterwerte mit den entsprechenden Optionsdefaults
/// und prüft die erwarteten Zahlenfelder im eingebetteten Referenzkatalog.
/// Der Timeout für das Vorlesen von Optionen wird separat auf 18 Sekunden geprüft.
/// </summary>
public sealed class SpeechCaptureSegmenterProfileParityTests
{
    private const string CatalogPath =
        "dialogmodell/reference-contracts/voice-interface/speech-capture-profile.reference.json";

    private static readonly string[] ExpectedFieldNames =
    [
        "speechAmplitudeThreshold",
        "speechStartAdaptiveMargin",
        "activePlaybackSpeechStartAdaptiveMargin",
        "idleAdaptiveFloorMaxMargin",
        "playbackLeakageBypassAmplitudeThreshold",
        "activePlaybackCalibrationChunkCount",
        "recentPlaybackCalibrationChunkCount",
        "maxPreRollChunks",
        "tailSilenceMs",
        "optionListingInitialSilenceTimeoutMs",
    ];

    [Fact]
    public void GetSegmenterProfile_EqualsInlineSegmenterOptionDefaults()
    {
        var defaults = new PcmSpeechCaptureSegmenterOptions();

        var profile = SpeechCaptureProfileCatalogReader.GetSegmenterProfile();

        Assert.NotNull(profile);
        Assert.Equal(defaults.SpeechAmplitudeThreshold, profile.SpeechAmplitudeThreshold);
        Assert.Equal(defaults.SpeechStartAdaptiveMargin, profile.SpeechStartAdaptiveMargin);
        Assert.Equal(defaults.ActivePlaybackSpeechStartAdaptiveMargin, profile.ActivePlaybackSpeechStartAdaptiveMargin);
        Assert.Equal(defaults.IdleAdaptiveFloorMaxMargin, profile.IdleAdaptiveFloorMaxMargin);
        Assert.Equal(defaults.PlaybackLeakageBypassAmplitudeThreshold, profile.PlaybackLeakageBypassAmplitudeThreshold);
        Assert.Equal(defaults.ActivePlaybackCalibrationChunkCount, profile.ActivePlaybackCalibrationChunkCount);
        Assert.Equal(defaults.RecentPlaybackCalibrationChunkCount, profile.RecentPlaybackCalibrationChunkCount);
        Assert.Equal(defaults.MaxPreRollChunks, profile.MaxPreRollChunks);
        Assert.Equal((int)defaults.TailSilence.TotalMilliseconds, profile.TailSilenceMs);
        Assert.Equal(18000, profile.OptionListingInitialSilenceTimeoutMs);
    }

    [Fact]
    public void EmbeddedNonActivationProfiles_DoNotCarryUnreadMaxUtteranceDuration()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(CatalogPath);
        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("captureProfilesByMode", out var profiles));

        string[] unwired =
        [
            "Listening",
            "ListeningContinuation",
            "PromptInterruption",
            "PromptInterruptionDetection",
            "ShortConfirmation",
        ];

        foreach (var profile in profiles.EnumerateArray())
        {
            var mode = profile.GetProperty("mode").GetString();
            if (!unwired.Contains(mode, StringComparer.Ordinal))
            {
                continue;
            }

            Assert.False(
                profile.TryGetProperty("maxUtteranceDurationMs", out _),
                $"Mode '{mode}' still models maxUtteranceDurationMs, which the runtime does not read.");
        }
    }

    [Fact]
    public void EmbeddedSegmenterReferenceBlock_ParsesAndCarriesAllTenFields()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(CatalogPath);
        using var document = JsonDocument.Parse(json);

        Assert.True(
            document.RootElement.TryGetProperty("segmenter", out var segmenter),
            "Embedded speech-capture-profile reference is missing the 'segmenter' block.");
        Assert.Equal(JsonValueKind.Object, segmenter.ValueKind);

        foreach (var fieldName in ExpectedFieldNames)
        {
            Assert.True(
                segmenter.TryGetProperty(fieldName, out var field),
                $"Embedded segmenter reference block is missing the '{fieldName}' field.");
            Assert.Equal(JsonValueKind.Number, field.ValueKind);
        }
    }
}
