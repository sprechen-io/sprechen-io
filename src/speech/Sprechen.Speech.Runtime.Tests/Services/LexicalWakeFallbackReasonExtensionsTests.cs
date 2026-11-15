using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die Umwandlung von Fallbackgründen in Schlüssel und Katalogwerte sowie die Rückrichtung.
/// Unbekannte Eingaben werden abgewiesen; Schlüssel und Katalogwerte sind nicht austauschbar.
/// </summary>
public sealed class LexicalWakeFallbackReasonExtensionsTests
{
    [Theory]
    [InlineData(LexicalWakeFallbackReason.CatalogFallbackOnly, "catalogFallbackOnly")]
    [InlineData(LexicalWakeFallbackReason.OverrideIneligible, "overrideIneligible")]
    [InlineData(LexicalWakeFallbackReason.RecognitionOnlyActivation, "recognitionOnlyActivation")]
    [InlineData(LexicalWakeFallbackReason.DetectorUnavailable, "detectorUnavailable")]
    [InlineData(LexicalWakeFallbackReason.DetectorWarmup, "detectorWarmup")]
    [InlineData(LexicalWakeFallbackReason.DetectorFailure, "detectorFailure")]
    public void ToWireCode_KnownValue_ReturnsCamelCaseSlotKey(
        LexicalWakeFallbackReason reason, string expected)
    {
        Assert.Equal(expected, reason.ToWireCode());
    }

    [Fact]
    public void ToWireCode_None_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => LexicalWakeFallbackReason.None.ToWireCode());

        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void ToWireCode_OutOfRangeCast_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ((LexicalWakeFallbackReason)999).ToWireCode());

        Assert.Equal("reason", ex.ParamName);
    }

    [Theory]
    [InlineData("catalogFallbackOnly", LexicalWakeFallbackReason.CatalogFallbackOnly)]
    [InlineData("overrideIneligible", LexicalWakeFallbackReason.OverrideIneligible)]
    [InlineData("recognitionOnlyActivation", LexicalWakeFallbackReason.RecognitionOnlyActivation)]
    [InlineData("detectorUnavailable", LexicalWakeFallbackReason.DetectorUnavailable)]
    [InlineData("detectorWarmup", LexicalWakeFallbackReason.DetectorWarmup)]
    [InlineData("detectorFailure", LexicalWakeFallbackReason.DetectorFailure)]
    public void TryParseWireCode_KnownSlotKey_ReturnsTrue(
        string wireCode, LexicalWakeFallbackReason expected)
    {
        Assert.True(LexicalWakeFallbackReasonExtensions.TryParseWireCode(wireCode, out var actual));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CatalogFallbackOnly")]
    [InlineData("WAKE_DETECTOR_CATALOG_FALLBACK_ONLY")]
    [InlineData("unknown")]
    public void TryParseWireCode_RejectedInput_ReturnsFalseAndNone(string? wireCode)
    {
        Assert.False(LexicalWakeFallbackReasonExtensions.TryParseWireCode(wireCode, out var actual));

        Assert.Equal(LexicalWakeFallbackReason.None, actual);
    }

    [Theory]
    [InlineData(LexicalWakeFallbackReason.CatalogFallbackOnly, "WAKE_DETECTOR_CATALOG_FALLBACK_ONLY")]
    [InlineData(LexicalWakeFallbackReason.OverrideIneligible, "WAKE_DETECTOR_OVERRIDE_INELIGIBLE")]
    [InlineData(LexicalWakeFallbackReason.RecognitionOnlyActivation, "WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION")]
    [InlineData(LexicalWakeFallbackReason.DetectorUnavailable, "WAKE_DETECTOR_UNAVAILABLE")]
    [InlineData(LexicalWakeFallbackReason.DetectorWarmup, "WAKE_DETECTOR_WARMUP")]
    [InlineData(LexicalWakeFallbackReason.DetectorFailure, "WAKE_DETECTOR_FAILURE")]
    public void ResolveCatalogReasonCode_KnownValue_ReturnsCatalogString(
        LexicalWakeFallbackReason reason, string expected)
    {
        var catalog = BuildReferenceCatalogReasonCodes();

        Assert.Equal(expected, reason.ResolveCatalogReasonCode(catalog));
    }

    [Fact]
    public void ResolveCatalogReasonCode_None_Throws()
    {
        var catalog = BuildReferenceCatalogReasonCodes();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => LexicalWakeFallbackReason.None.ResolveCatalogReasonCode(catalog));

        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void ResolveCatalogReasonCode_OutOfRangeCast_Throws()
    {
        var catalog = BuildReferenceCatalogReasonCodes();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => ((LexicalWakeFallbackReason)999).ResolveCatalogReasonCode(catalog));

        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void ResolveCatalogReasonCode_NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => LexicalWakeFallbackReason.DetectorUnavailable.ResolveCatalogReasonCode(null!));
    }

    [Theory]
    [InlineData("WAKE_DETECTOR_CATALOG_FALLBACK_ONLY", LexicalWakeFallbackReason.CatalogFallbackOnly)]
    [InlineData("WAKE_DETECTOR_OVERRIDE_INELIGIBLE", LexicalWakeFallbackReason.OverrideIneligible)]
    [InlineData("WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION", LexicalWakeFallbackReason.RecognitionOnlyActivation)]
    [InlineData("WAKE_DETECTOR_UNAVAILABLE", LexicalWakeFallbackReason.DetectorUnavailable)]
    [InlineData("WAKE_DETECTOR_WARMUP", LexicalWakeFallbackReason.DetectorWarmup)]
    [InlineData("WAKE_DETECTOR_FAILURE", LexicalWakeFallbackReason.DetectorFailure)]
    public void TryFromCatalogReasonCode_KnownCatalogValue_ReturnsTrueAndMember(
        string reasonCode, LexicalWakeFallbackReason expected)
    {
        var catalog = BuildReferenceCatalogReasonCodes();

        Assert.True(LexicalWakeFallbackReasonExtensions.TryFromCatalogReasonCode(reasonCode, catalog, out var actual));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("catalogFallbackOnly")]
    [InlineData("CatalogFallbackOnly")]
    [InlineData("WAKE_DETECTOR_UNKNOWN")]
    public void TryFromCatalogReasonCode_RejectedInput_ReturnsFalseAndNone(string? reasonCode)
    {
        var catalog = BuildReferenceCatalogReasonCodes();

        Assert.False(LexicalWakeFallbackReasonExtensions.TryFromCatalogReasonCode(reasonCode, catalog, out var actual));

        Assert.Equal(LexicalWakeFallbackReason.None, actual);
    }

    [Fact]
    public void TryFromCatalogReasonCode_NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => LexicalWakeFallbackReasonExtensions.TryFromCatalogReasonCode(
                "WAKE_DETECTOR_UNAVAILABLE", null!, out _));
    }

    private static WakeWordDetectorFallbackReasonCodes BuildReferenceCatalogReasonCodes()
    {
        return new WakeWordDetectorFallbackReasonCodes
        {
            CatalogFallbackOnly = "WAKE_DETECTOR_CATALOG_FALLBACK_ONLY",
            OverrideIneligible = "WAKE_DETECTOR_OVERRIDE_INELIGIBLE",
            RecognitionOnlyActivation = "WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION",
            DetectorUnavailable = "WAKE_DETECTOR_UNAVAILABLE",
            DetectorWarmup = "WAKE_DETECTOR_WARMUP",
            DetectorFailure = "WAKE_DETECTOR_FAILURE",
        };
    }
}
