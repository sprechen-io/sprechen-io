using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class TestAcousticWakeWordDetectorConfigurationResolver : IAcousticWakeWordDetectorConfigurationResolver
{
    private static readonly WakeWordDetectorProfile DetectorProfile = new()
    {
        DetectorId = "voice-test-unavailable",
        OperatingMode = WakeWordDetectorOperatingMode.AcousticPreferred,
        ConfidenceThreshold = 0.8,
        RefractoryMs = 0,
        WarmupMs = 0,
        LookbackMs = 0,
        EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
        FallbackReasonCodes = new WakeWordDetectorFallbackReasonCodes
        {
            CatalogFallbackOnly = "catalogFallbackOnly",
            OverrideIneligible = "overrideIneligible",
            RecognitionOnlyActivation = "recognitionOnlyActivation",
            DetectorUnavailable = "detectorUnavailable",
            DetectorWarmup = "detectorWarmup",
            DetectorFailure = "detectorFailure",
        },
    };

    public ValueTask<AcousticWakeWordDetectorConfiguration> ResolveAsync(
        string isoCode,
        WakeWordInventory wakeWordInventory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new AcousticWakeWordDetectorConfiguration(
            isoCode.Trim().ToLowerInvariant(),
            wakeWordInventory,
            DetectorProfile,
            RequiresLexicalFallback: false,
            LexicalFallbackReasonCode: string.Empty));
    }

    public AcousticWakeWordDetectorConfiguration ResolveForLexicalFallback(
        LexicalWakeFallbackReason reason,
        AcousticWakeWordDetectorConfiguration baseConfiguration)
    {
        ArgumentNullException.ThrowIfNull(baseConfiguration);

        return baseConfiguration with
        {
            RequiresLexicalFallback = true,
            LexicalFallbackReasonCode = reason.ResolveCatalogReasonCode(DetectorProfile.FallbackReasonCodes),
        };
    }
}
