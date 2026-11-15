using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class GenericPromptCatalogPreflightValidationTests
{
    [Fact]
    public void ValidatePromptCatalog_WhenStrictPromptTemplateIsMissing_ReturnsLocalizationError()
    {
        var errors = new List<PreflightIssue>();
        var catalog = new GenericPromptCatalog
        {
            PromptCatalogModelId = "generic.prompts",
            PromptsByLanguage = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
            {
                ["de"] = new(StringComparer.Ordinal)
                {
                    ["dialog.noErrors"] = "Keine Fehler."
                },
                ["en"] = new(StringComparer.Ordinal)
            }
        };

        GenericPromptCatalogPreflightValidation.ValidatePromptCatalog(
            errors,
            ["de", "en"],
            defaultLanguage: "de",
            strictLocalization: true,
            catalog,
            "$.promptCatalog.promptsByLanguage");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLocalizationMissing
                && error.Path == "$.promptCatalog.promptsByLanguage.en"
                && error.Id == "dialog.noErrors");
    }
}
