using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class VoiceLanguageProfilePreflightValidationTests
{
    [Fact]
    public void ValidateLanguageProfilesExactMatch_WhenProfileLanguageIsMissing_ReturnsInvariantError()
    {
        var errors = new List<PreflightIssue>();

        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfilesExactMatch(
            errors,
            ["de", "en"],
            ["de"],
            "$.profiles",
            "voice-interface",
            "test profile");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLanguageInvariantBroken
                && error.Path == "$.profiles"
                && error.Id == "voice-interface");
    }
}
