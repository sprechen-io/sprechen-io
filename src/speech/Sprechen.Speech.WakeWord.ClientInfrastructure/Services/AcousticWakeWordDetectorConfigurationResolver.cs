using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Verbindet das ausgewählte Detektorprofil mit dem aktuellen Aktivierungsinventar,
/// damit ungeeignete Inventare anhand der modellierten Regeln lexikalisch geprüft werden.
/// </summary>
internal sealed class AcousticWakeWordDetectorConfigurationResolver : IAcousticWakeWordDetectorConfigurationResolver
{
    private readonly IWakeWordDetectorProfileSelector _profileSelector;

    public AcousticWakeWordDetectorConfigurationResolver(IWakeWordDetectorProfileSelector profileSelector)
    {
        _profileSelector = profileSelector ?? throw new ArgumentNullException(nameof(profileSelector));
    }

    public AcousticWakeWordDetectorConfiguration ResolveForLexicalFallback(
        LexicalWakeFallbackReason reason,
        AcousticWakeWordDetectorConfiguration baseConfiguration)
    {
        ArgumentNullException.ThrowIfNull(baseConfiguration);

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                $"Unknown LexicalWakeFallbackReason value '{(int)reason}'.");
        }

        if (reason == LexicalWakeFallbackReason.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "LexicalWakeFallbackReason.None is a sentinel meaning 'no fallback active' and must not be projected to a fallback configuration.");
        }

        var resolvedWireCode = reason
            .ResolveCatalogReasonCode(baseConfiguration.DetectorProfile.FallbackReasonCodes);

        return baseConfiguration with
        {
            RequiresLexicalFallback = true,
            LexicalFallbackReasonCode = string.IsNullOrWhiteSpace(resolvedWireCode)
                ? baseConfiguration.LexicalFallbackReasonCode
                : resolvedWireCode.Trim(),
        };
    }

    public ValueTask<AcousticWakeWordDetectorConfiguration> ResolveAsync(
        string isoCode,
        WakeWordInventory wakeWordInventory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedIsoCode = IsoCode.NormalizeIso6391(isoCode);
        var detectorCatalog = VoiceInterfaceModelCatalog.WakeWord.DetectorCatalog;
        var profile = ResolveDetectorProfile(detectorCatalog, normalizedIsoCode, wakeWordInventory);
        var fallbackReason = ResolveLexicalFallbackReason(
            normalizedIsoCode,
            wakeWordInventory,
            profile);

        var fallbackReasonCode = fallbackReason == LexicalWakeFallbackReason.None
            ? string.Empty
            : fallbackReason.ResolveCatalogReasonCode(profile.FallbackReasonCodes);

        return ValueTask.FromResult(
            new AcousticWakeWordDetectorConfiguration(
                normalizedIsoCode,
                wakeWordInventory,
                profile,
                fallbackReason != LexicalWakeFallbackReason.None,
                fallbackReasonCode));
    }

    private WakeWordDetectorProfile ResolveDetectorProfile(
        WakeWordDetectorCatalog detectorCatalog,
        string normalizedIsoCode,
        WakeWordInventory wakeWordInventory)
    {
        ArgumentNullException.ThrowIfNull(detectorCatalog);

        if (detectorCatalog.DetectorProfiles.Count == 0)
        {
            throw new InvalidOperationException("Wake-word detector catalog must contain at least one detector profile.");
        }

        var context = new WakeWordDetectorProfileSelectionContext(normalizedIsoCode, wakeWordInventory);
        return _profileSelector.Select(detectorCatalog.DetectorProfiles, context);
    }

    private static LexicalWakeFallbackReason ResolveLexicalFallbackReason(
        string normalizedIsoCode,
        WakeWordInventory wakeWordInventory,
        WakeWordDetectorProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedIsoCode);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.OperatingMode == WakeWordDetectorOperatingMode.LexicalFallbackOnly)
        {
            return LexicalWakeFallbackReason.CatalogFallbackOnly;
        }

        if (wakeWordInventory.RecognitionOnlyActivationForms.Count > 0)
        {
            return LexicalWakeFallbackReason.RecognitionOnlyActivation;
        }

        if (ContainsMultiWordActivation(wakeWordInventory.ActivationForms))
        {
            return LexicalWakeFallbackReason.CatalogFallbackOnly;
        }

        if (profile.EligibilityPolicy == WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly
            && !MatchesModeledDefaultInventory(normalizedIsoCode, wakeWordInventory.ActivationForms))
        {
            return LexicalWakeFallbackReason.OverrideIneligible;
        }

        return LexicalWakeFallbackReason.None;
    }

    private static bool MatchesModeledDefaultInventory(
        string normalizedIsoCode,
        IReadOnlyList<string> activationForms)
    {
        var modeledActivationForms = VoiceInterfaceModelCatalog
            .WakeWord.GetActivationFormsForLanguage(normalizedIsoCode)
            .ToArray();

        var normalizedRuntime = new WakeWordActivationFormCodec().Normalize(activationForms);
        var normalizedModeled = new WakeWordActivationFormCodec().Normalize(modeledActivationForms);

        if (normalizedRuntime.Length != normalizedModeled.Length)
        {
            return false;
        }

        for (var index = 0; index < normalizedRuntime.Length; index++)
        {
            if (!string.Equals(
                    normalizedRuntime[index],
                    normalizedModeled[index],
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsMultiWordActivation(IReadOnlyList<string> activationForms)
    {
        ArgumentNullException.ThrowIfNull(activationForms);

        foreach (var activationForm in activationForms)
        {
            if (string.IsNullOrWhiteSpace(activationForm))
            {
                continue;
            }

            foreach (var character in activationForm)
            {
                if (char.IsWhiteSpace(character))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
