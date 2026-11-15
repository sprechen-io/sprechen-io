using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.SpeechCapture;
using Sprechen.Mda.Contracts.V1.Variables;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class MdaCatalogProfilePreflightOwnerTests
{
    [Fact]
    public void CatalogLocalizationValidator_WhenFormalVariantMissing_ReturnsFormalMissingIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new CatalogLocalizationPreflightValidator();

        validator.Validate(
            errors,
            ["de"],
            defaultLanguage: "de",
            strictLocalization: true,
            new LocalizedText { ["de@informal"] = "hallo" },
            "$.prompts[0].text",
            "prompt.help",
            PreflightCodes.ErrorLocalizationMissing);

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLocalizationMissing
                && error.Message == "Missing formal localization for language 'de'."
                && error.Path == "$.prompts[0].text"
                && error.Id == "prompt.help");
    }

    [Fact]
    public void SpeechCaptureModeProfileValidator_WhenModeIsDuplicated_ReturnsDuplicateIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new SpeechCaptureModeProfileValidator();

        validator.Validate(
            errors,
            [
                CreateCaptureProfile("wake"),
                CreateCaptureProfile("wake")
            ]);

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorIdDuplicate
                && error.Message == "mode 'wake' is duplicated."
                && error.Path == "$.captureProfilesByMode[1].mode");
    }

    [Theory]
    [InlineData(999)]
    [InlineData(60001)]
    public void SpeechCaptureSegmenterValidator_WhenOptionListingInitialSilenceTimeoutOutOfRange_ReturnsSettingsIssue(
        int value)
    {
        var errors = new List<PreflightIssue>();
        var validator = new SpeechCaptureSegmenterValidator();
        var segmenter = CreateSegmenterProfile(optionListingInitialSilenceTimeoutMs: value);

        validator.Validate(errors, segmenter);

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorSettingsInvalid
                && error.Message == $"segmenter.optionListingInitialSilenceTimeoutMs must be between 1000 and 60000, got {value}."
                && error.Path == "$.segmenter.optionListingInitialSilenceTimeoutMs");
    }

    [Fact]
    public void ApplicationSelectionPromptValidator_WhenPromptIdDiffersOnlyByCase_ReturnsDuplicateIssue()
    {
        var errors = new List<PreflightIssue>();
        var localizationIssues = new List<PreflightIssue>();
        var validator = new ApplicationSelectionPromptPreflightValidator();
        var model = new ApplicationSelectionDialog
        {
            SupportedLanguages = ["de"],
            StrictLocalization = true,
            Prompts =
            [
                new CatalogVoicePrompt { PromptId = "app.help", Text = new LocalizedText { ["de"] = "hilfe" } },
                new CatalogVoicePrompt { PromptId = "APP.HELP", Text = new LocalizedText { ["de"] = "hilfe erneut" } },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.app.confirmation.repeat",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                        ["de@informal"] = "Bitte sag ja oder nein."
                    }
                },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.form.confirmation.repeat",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                        ["de@informal"] = "Bitte sag ja oder nein."
                    }
                }
            ]
        };

        validator.Validate(errors, localizationIssues, model, defaultLanguage: "de", localizationCode: PreflightCodes.ErrorLocalizationMissing);

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorIdDuplicate
                && error.Path == "$.prompts[1].promptId"
                && error.Id == "APP.HELP");
    }

    [Fact]
    public void ReusableVariableSensitiveDataPolicyValidator_WhenSecretAllowsVoiceInput_ReturnsSettingsIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new ReusableVariableSensitiveDataPolicyValidator();
        var definition = CreateReusableVariable("secretVariable");
        definition.PrivacyClass = PrivacyClass.Secret;
        definition.SensitiveDataPolicy = new SensitiveDataPolicy
        {
            VoiceInputPolicy = "recommended"
        };

        validator.Validate(errors, definition, definitionIndex: 0);

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorSettingsInvalid
                && error.Path == "$.definitions[0].sensitiveDataPolicy.voiceInputPolicy"
                && error.Id == "secretVariable");
    }

    [Fact]
    public void VoiceInterfaceCatalogReferenceValidator_WhenPromptModelIdDiffers_ReturnsReferenceIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new CatalogReferenceEqualityPreflightValidator();

        validator.Validate(
            errors,
            referencedModelId: "voice.prompts",
            loadedModelId: "other.prompts",
            referencedVersion: "1",
            loadedVersion: "1",
            modelIdPath: "$.promptCatalog.promptCatalogModelId",
            versionPath: "$.promptCatalog.catalogVersion",
            modelIdNoun: "promptCatalogModelId",
            versionNoun: "prompt catalog version");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorRefMissing
                && error.Path == "$.promptCatalog.promptCatalogModelId"
                && error.Id == "voice.prompts");
    }

    [Fact]
    public void WakeWordActivationFormNormalizer_NormalizesDiacriticsAndSeparatorsToTokens()
    {
        var normalizer = new WakeWordActivationFormNormalizer();

        var tokens = normalizer.NormalizeTokens("  SpRe-chen!! IO  ");

        Assert.Equal(["spre", "chen", "io"], tokens);
    }

    [Fact]
    public void GenericPromptLanguageMapValidator_WhenDefaultLanguageIsMissing_ReturnsInvariantIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new GenericPromptLanguageMapValidator();

        validator.Validate(
            errors,
            ["de", "en"],
            defaultLanguage: "de",
            promptsByLanguage: new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
            {
                ["en"] = new(StringComparer.Ordinal)
            },
            path: "$.promptCatalog.promptsByLanguage",
            id: "generic.prompts");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorLanguageInvariantBroken
                && error.Message == "Prompt catalog must contain the defaultLanguage 'de'."
                && error.Id == "generic.prompts");
    }

    [Fact]
    public void VoiceResourceCatalogResourceValidator_WhenResourceIdIsDuplicated_ReturnsDuplicateIssue()
    {
        var errors = new List<PreflightIssue>();
        var validator = new VoiceResourceCatalogResourceValidator();

        validator.Validate(
            errors,
            ["de"],
            strictLocalization: false,
            resources:
            [
                new VoiceInteractionResource { Id = "prompt.help", Texts = new LocalizedText { ["de"] = "hilfe" } },
                new VoiceInteractionResource { Id = "prompt.help", Texts = new LocalizedText { ["de"] = "hilfe erneut" } }
            ],
            path: "$.resources");

        Assert.Contains(
            errors,
            error => error.Code == PreflightCodes.ErrorIdDuplicate
                && error.Path == "$.resources[1].id"
                && error.Id == "prompt.help");
    }

    private static SpeechCaptureProfile CreateCaptureProfile(string mode)
    {
        return new SpeechCaptureProfile
        {
            Mode = mode,
            MaxUtteranceDurationMs = 1
        };
    }

    private static SpeechCaptureSegmenterProfile CreateSegmenterProfile(int optionListingInitialSilenceTimeoutMs)
    {
        return new SpeechCaptureSegmenterProfile
        {
            SpeechAmplitudeThreshold = 350,
            SpeechStartAdaptiveMargin = 120,
            ActivePlaybackSpeechStartAdaptiveMargin = 220,
            IdleAdaptiveFloorMaxMargin = 300,
            PlaybackLeakageBypassAmplitudeThreshold = 1000,
            ActivePlaybackCalibrationChunkCount = 8,
            RecentPlaybackCalibrationChunkCount = 2,
            MaxPreRollChunks = 25,
            TailSilenceMs = 500,
            OptionListingInitialSilenceTimeoutMs = optionListingInitialSilenceTimeoutMs
        };
    }

    private static ReusableVariableDefinition CreateReusableVariable(string variableDefinitionId)
    {
        return new ReusableVariableDefinition
        {
            VariableDefinitionId = variableDefinitionId,
            Name = variableDefinitionId,
            ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
        };
    }
}
