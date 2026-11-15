using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class WakeWordCatalogProfilePreflightValidationTests
{
    [Fact]
    public void ValidateActivationEntries_WhenCommandAnchorLeadInNormalizesToNoTokens_ReturnsInvariantError()
    {
        var errors = new List<PreflightIssue>();
        var catalog = new WakeWordCatalog
        {
            CommandAnchorLeadIns =
            [
                new CommandAnchorLeadIn { LeadIn = "   ", Role = CommandAnchorLeadInRoles.Activation }
            ],
            ActivationProfilesByLanguage =
            [
                new WakeWordLanguageProfile { LanguageCode = "de", WakeWordName = "guide" }
            ]
        };

        WakeWordCatalogProfilePreflightValidation.ValidateActivationEntries(
            errors,
            catalog,
            "$",
            "wake-words");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLanguageInvariantBroken
                && error.Path == "$.commandAnchorLeadIns[0].leadIn"
                && error.Id == "wake-words");
    }

    [Fact]
    public void ValidateActivationEntries_WhenWakeWordNameNormalizesToNoTokens_ReturnsInvariantError()
    {
        var errors = new List<PreflightIssue>();
        var catalog = new WakeWordCatalog
        {
            CommandAnchorLeadIns =
            [
                new CommandAnchorLeadIn { LeadIn = "okay", Role = CommandAnchorLeadInRoles.Activation }
            ],
            ActivationProfilesByLanguage =
            [
                new WakeWordLanguageProfile { LanguageCode = "de", WakeWordName = "   " }
            ]
        };

        WakeWordCatalogProfilePreflightValidation.ValidateActivationEntries(
            errors,
            catalog,
            "$",
            "wake-words");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLanguageInvariantBroken
                && error.Path == "$.activationProfilesByLanguage[0].wakeWordName"
                && error.Id == "wake-words");
    }

    [Fact]
    public void ValidateActivationEntries_WhenCatalogIsSound_ReturnsNoError()
    {
        var errors = new List<PreflightIssue>();
        var catalog = new WakeWordCatalog
        {
            CommandAnchorLeadIns =
            [
                new CommandAnchorLeadIn { LeadIn = "okay", Role = CommandAnchorLeadInRoles.Activation },
                new CommandAnchorLeadIn { LeadIn = "ok", Role = CommandAnchorLeadInRoles.RecognitionOnly }
            ],
            ActivationProfilesByLanguage =
            [
                new WakeWordLanguageProfile { LanguageCode = "de", WakeWordName = "guide" }
            ]
        };

        WakeWordCatalogProfilePreflightValidation.ValidateActivationEntries(
            errors,
            catalog,
            "$",
            "wake-words");

        Assert.Empty(errors);
    }
}
