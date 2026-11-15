using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class WakeWordDetectorCatalogReferencePreflightValidationTests
{
    [Fact]
    public void ValidateDetectorTargetsWakeWordCatalog_WhenWakeWordModelDoesNotMatch_ReturnsRefMissingError()
    {
        var errors = new List<PreflightIssue>();
        var wakeWordCatalog = new WakeWordCatalog
        {
            WakeWordModelId = "default.voiceWakeWords"
        };
        var detectorCatalog = new WakeWordDetectorCatalog
        {
            DetectorCatalogModelId = "default.voiceWakeWordDetectors",
            WakeWordModelId = "other.voiceWakeWords"
        };

        WakeWordDetectorCatalogReferencePreflightValidation.ValidateDetectorTargetsWakeWordCatalog(
            errors,
            wakeWordCatalog,
            detectorCatalog,
            "$.wakeWordDetectorCatalog");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorRefMissing
                && error.Path == "$.wakeWordDetectorCatalog.wakeWordModelId"
                && error.Id == "default.voiceWakeWordDetectors");
    }
}
