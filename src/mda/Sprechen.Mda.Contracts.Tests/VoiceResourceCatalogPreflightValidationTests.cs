using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class VoiceResourceCatalogPreflightValidationTests
{
    [Fact]
    public void ValidateResources_WhenStrictLocalizationIsMissing_ReturnsLocalizationError()
    {
        var errors = new List<PreflightIssue>();
        var resources = new List<VoiceInteractionResource>
        {
            new()
            {
                Id = "prompt.test",
                Texts = new LocalizedText
                {
                    ["de"] = "Hallo"
                }
            }
        };

        VoiceResourceCatalogPreflightValidation.ValidateResources(
            errors,
            ["de", "en"],
            strictLocalization: true,
            resources,
            "$.resources");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLocalizationMissing
                && error.Path == "$.resources[0].texts"
                && error.Id == "prompt.test");
    }
}
