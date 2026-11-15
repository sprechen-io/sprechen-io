using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft doppelte Detektorkennungen, Konfidenz- und Zeitwerte
/// sowie erforderliche Begründungscodes für Erkennungsersatzverfahren.
/// </summary>
internal sealed class WakeWordDetectorCatalogPreflightValidator
{
    public PreflightResult Validate(WakeWordDetectorCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        var seenDetectorIds = new HashSet<string>(StringComparer.Ordinal);
        for (var profileIndex = 0; profileIndex < catalog.DetectorProfiles.Count; profileIndex++)
        {
            var profile = catalog.DetectorProfiles[profileIndex];
            var pathPrefix = $"$.detectorProfiles[{profileIndex}]";

            if (!seenDetectorIds.Add(profile.DetectorId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate detectorId '{profile.DetectorId}'.",
                    Path: $"{pathPrefix}.detectorId",
                    Id: profile.DetectorId));
            }

            if (profile.ConfidenceThreshold is < 0 or > 1)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorValueOutOfRange,
                    Message: $"confidenceThreshold must be within [0, 1], got '{profile.ConfidenceThreshold}'.",
                    Path: $"{pathPrefix}.confidenceThreshold",
                    Id: catalog.DetectorCatalogModelId));
            }

            ValidateNonNegative(errors, profile.RefractoryMs, $"{pathPrefix}.refractoryMs", catalog.DetectorCatalogModelId);
            ValidateNonNegative(errors, profile.WarmupMs, $"{pathPrefix}.warmupMs", catalog.DetectorCatalogModelId);
            ValidateNonNegative(errors, profile.LookbackMs, $"{pathPrefix}.lookbackMs", catalog.DetectorCatalogModelId);
            ValidateFallbackReasonCodes(errors, profile.FallbackReasonCodes, $"{pathPrefix}.fallbackReasonCodes", catalog.DetectorCatalogModelId);
        }

        return new PreflightResult(errors, warnings);
    }

    private static void ValidateNonNegative(
        List<PreflightIssue> errors,
        int value,
        string path,
        string? catalogId)
    {
        if (value >= 0)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorValueOutOfRange,
            Message: $"'{path}' must be non-negative.",
            Path: path,
            Id: catalogId));
    }

    private static void ValidateFallbackReasonCodes(
        List<PreflightIssue> errors,
        WakeWordDetectorFallbackReasonCodes codes,
        string path,
        string? catalogId)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ValidateRequiredCode(errors, codes.CatalogFallbackOnly, $"{path}.catalogFallbackOnly", catalogId);
        ValidateRequiredCode(errors, codes.OverrideIneligible, $"{path}.overrideIneligible", catalogId);
        ValidateRequiredCode(errors, codes.RecognitionOnlyActivation, $"{path}.recognitionOnlyActivation", catalogId);
        ValidateRequiredCode(errors, codes.DetectorUnavailable, $"{path}.detectorUnavailable", catalogId);
        ValidateRequiredCode(errors, codes.DetectorWarmup, $"{path}.detectorWarmup", catalogId);
        ValidateRequiredCode(errors, codes.DetectorFailure, $"{path}.detectorFailure", catalogId);
    }

    private static void ValidateRequiredCode(
        List<PreflightIssue> errors,
        string value,
        string path,
        string? catalogId)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorIdMissing,
            Message: $"Fallback reason code at '{path}' must be non-empty.",
            Path: path,
            Id: catalogId));
    }
}
