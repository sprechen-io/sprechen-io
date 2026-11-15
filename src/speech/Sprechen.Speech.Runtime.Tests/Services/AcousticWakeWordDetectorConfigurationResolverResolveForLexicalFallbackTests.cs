using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die sechs aufgeführten Rückfallgründe und ihre übertragenen Codes.
/// Beim Ableiten bleiben Sprache, Inventar und Detektorprofil erhalten; None,
/// unbekannte Enum-Werte und eine fehlende Basiskonfiguration werden abgewiesen.
/// </summary>
public sealed class AcousticWakeWordDetectorConfigurationResolverResolveForLexicalFallbackTests
{
    [Theory]
    [InlineData(LexicalWakeFallbackReason.CatalogFallbackOnly, "WAKE_DETECTOR_CATALOG_FALLBACK_ONLY")]
    [InlineData(LexicalWakeFallbackReason.OverrideIneligible, "WAKE_DETECTOR_OVERRIDE_INELIGIBLE")]
    [InlineData(LexicalWakeFallbackReason.RecognitionOnlyActivation, "WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION")]
    [InlineData(LexicalWakeFallbackReason.DetectorUnavailable, "WAKE_DETECTOR_UNAVAILABLE")]
    [InlineData(LexicalWakeFallbackReason.DetectorWarmup, "WAKE_DETECTOR_WARMUP")]
    [InlineData(LexicalWakeFallbackReason.DetectorFailure, "WAKE_DETECTOR_FAILURE")]
    public void ResolveForLexicalFallback_ForEachNonNoneEnumValue_FlipsRequiresFallbackAndResolvesWireCode(
        LexicalWakeFallbackReason reason,
        string expectedWireCode)
    {
        var resolver = CreateResolver();
        var baseConfiguration = BuildBaseConfiguration();

        var result = resolver.ResolveForLexicalFallback(reason, baseConfiguration);

        Assert.True(result.RequiresLexicalFallback);
        Assert.Equal(expectedWireCode, result.LexicalFallbackReasonCode);
    }

    [Fact]
    public void ResolveForLexicalFallback_PreservesNonDerivedFields()
    {
        var resolver = CreateResolver();
        var baseConfiguration = BuildBaseConfiguration();

        var result = resolver.ResolveForLexicalFallback(
            LexicalWakeFallbackReason.DetectorUnavailable,
            baseConfiguration);

        Assert.Equal(baseConfiguration.LanguageCode, result.LanguageCode);
        Assert.Same(baseConfiguration.WakeWordInventory, result.WakeWordInventory);
        Assert.Same(baseConfiguration.DetectorProfile, result.DetectorProfile);
    }

    [Fact]
    public void ResolveForLexicalFallback_IsIdempotent()
    {
        var resolver = CreateResolver();
        var baseConfiguration = BuildBaseConfiguration();

        var first = resolver.ResolveForLexicalFallback(
            LexicalWakeFallbackReason.DetectorWarmup,
            baseConfiguration);
        var second = resolver.ResolveForLexicalFallback(
            LexicalWakeFallbackReason.DetectorWarmup,
            first);

        Assert.Equal(first, second);
        Assert.True(second.RequiresLexicalFallback);
        Assert.Equal("WAKE_DETECTOR_WARMUP", second.LexicalFallbackReasonCode);
    }

    [Fact]
    public void ResolveForLexicalFallback_WhenReasonIsNone_ThrowsArgumentOutOfRangeException()
    {
        var resolver = CreateResolver();
        var baseConfiguration = BuildBaseConfiguration();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            resolver.ResolveForLexicalFallback(LexicalWakeFallbackReason.None, baseConfiguration));
        Assert.Equal("reason", ex.ParamName);
    }

    [Fact]
    public void ResolveForLexicalFallback_WhenBaseConfigurationIsNull_ThrowsArgumentNullException()
    {
        var resolver = CreateResolver();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            resolver.ResolveForLexicalFallback(
                LexicalWakeFallbackReason.DetectorUnavailable,
                baseConfiguration: null!));
        Assert.Equal("baseConfiguration", ex.ParamName);
    }

    [Fact]
    public void ResolveForLexicalFallback_WhenReasonIsUndefinedEnumCast_ThrowsArgumentOutOfRangeException()
    {
        var resolver = CreateResolver();
        var baseConfiguration = BuildBaseConfiguration();

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            resolver.ResolveForLexicalFallback((LexicalWakeFallbackReason)999, baseConfiguration));
        Assert.Equal("reason", ex.ParamName);
    }

    private static AcousticWakeWordDetectorConfigurationResolver CreateResolver()
    {
        return new AcousticWakeWordDetectorConfigurationResolver(new FirstProfileSelector());
    }

    private static AcousticWakeWordDetectorConfiguration BuildBaseConfiguration()
    {
        var profile = new WakeWordDetectorProfile
        {
            DetectorId = "acoustic.okay",
            OperatingMode = WakeWordDetectorOperatingMode.AcousticPreferred,
            ConfidenceThreshold = 0.85,
            RefractoryMs = 1500,
            WarmupMs = 0,
            LookbackMs = 1500,
            EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
            FallbackReasonCodes = new WakeWordDetectorFallbackReasonCodes
            {
                CatalogFallbackOnly = "WAKE_DETECTOR_CATALOG_FALLBACK_ONLY",
                OverrideIneligible = "WAKE_DETECTOR_OVERRIDE_INELIGIBLE",
                RecognitionOnlyActivation = "WAKE_DETECTOR_RECOGNITION_ONLY_ACTIVATION",
                DetectorUnavailable = "WAKE_DETECTOR_UNAVAILABLE",
                DetectorWarmup = "WAKE_DETECTOR_WARMUP",
                DetectorFailure = "WAKE_DETECTOR_FAILURE",
            },
        };

        var inventory = new WakeWordInventory(
            activationForms: new List<string> { "Okay" },
            recognitionOnlyActivationForms: new List<string>());

        return new AcousticWakeWordDetectorConfiguration(
            LanguageCode: "de",
            WakeWordInventory: inventory,
            DetectorProfile: profile,
            RequiresLexicalFallback: false,
            LexicalFallbackReasonCode: string.Empty);
    }
}
