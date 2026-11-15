using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft den Detektorkatalog und seine Referenz auf den geladenen Aktivierungswortkatalog.
/// </summary>
internal static class WakeWordDetectorCatalogReferencePreflightValidation
{
    internal static void ValidateDetectorTargetsWakeWordCatalog(
        List<PreflightIssue> errors,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog detectorCatalog,
        string path)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(wakeWordCatalog);
        ArgumentNullException.ThrowIfNull(detectorCatalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var detectorPreflight = WakeWordDetectorCatalogPreflight.Validate(detectorCatalog);
        errors.AddRange(detectorPreflight.Errors);

        if (string.Equals(detectorCatalog.WakeWordModelId, wakeWordCatalog.WakeWordModelId, StringComparison.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"Wake-word detector catalog references wakeWordModelId '{detectorCatalog.WakeWordModelId}', but the loaded wake-word catalog is '{wakeWordCatalog.WakeWordModelId}'.",
            Path: $"{path}.wakeWordModelId",
            Id: detectorCatalog.DetectorCatalogModelId));
    }
}
