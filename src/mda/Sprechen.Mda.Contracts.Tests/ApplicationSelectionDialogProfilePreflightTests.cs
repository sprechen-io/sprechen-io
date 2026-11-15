using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class ApplicationSelectionDialogProfilePreflightTests
{
    [Theory]
    [InlineData(-1, null, "sttMinConfidenceToAccept")]
    [InlineData(101, null, "sttMinConfidenceToAccept")]
    [InlineData(null, -1, "sttMinConfidenceToConfirm")]
    [InlineData(null, 101, "sttMinConfidenceToConfirm")]
    public void Validate_WhenConfidenceThresholdOutsidePercentRange_ReturnsError(
        int? acceptThreshold,
        int? confirmThreshold,
        string invalidProperty)
    {
        var profile = CreateProfile(acceptThreshold, confirmThreshold);

        var result = ApplicationSelectionDialogProfilePreflight.Validate(profile);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Id == profile.ProfileId
            && error.Path == $"$.{invalidProperty}");
    }

    [Fact]
    public void Validate_WhenBothConfidenceThresholdsAreInverted_ReturnsError()
    {
        var profile = CreateProfile(64, 65);

        var result = ApplicationSelectionDialogProfilePreflight.Validate(profile);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Path == "$.sttMinConfidenceToConfirm");
    }

    [Theory]
    [InlineData(50, null)]
    [InlineData(null, 90)]
    [InlineData(85, 65)]
    public void Validate_WhenSparseOrOrderedConfidenceThresholdsAreValid_ReturnsOk(
        int? acceptThreshold,
        int? confirmThreshold)
    {
        var profile = CreateProfile(acceptThreshold, confirmThreshold);

        var result = ApplicationSelectionDialogProfilePreflight.Validate(profile);

        Assert.True(result.Ok);
        Assert.DoesNotContain(result.Errors, error => error.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    private static ApplicationSelectionDialogProfile CreateProfile(
        int? acceptThreshold,
        int? confirmThreshold)
    {
        return new ApplicationSelectionDialogProfile
        {
            SchemaVersion = "1",
            ProfileVersion = "1",
            ProfileId = "test.selection-dialog-profile",
            SttMinConfidenceToAccept = acceptThreshold,
            SttMinConfidenceToConfirm = confirmThreshold
        };
    }
}
