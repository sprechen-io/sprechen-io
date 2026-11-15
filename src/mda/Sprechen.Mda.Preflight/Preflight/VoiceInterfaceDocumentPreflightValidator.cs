using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Referenzkennungen, Versionen, Sprachabdeckung und ausgewählte Inhalte
/// des Sprachoberflächendokuments mit seinen geladenen Katalogen.
/// </summary>
internal sealed class VoiceInterfaceDocumentPreflightValidator
{
    private readonly CatalogReferenceEqualityPreflightValidator _catalogReferenceValidator = new();
    private readonly VoiceLexiconProfileSetPreflightValidator _lexiconProfileSetValidator = new();
    private readonly VoiceInterfacePreflightValidator _voiceInterfaceValidator = new();

    public PreflightResult Validate(
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog,
        GenericPromptCatalog promptCatalog,
        VoiceLexiconCatalog lexiconCatalog,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        var errors = new List<PreflightIssue>();

        _voiceInterfaceValidator.ValidateSpeechOutput(errors, document.FirstPcmEnqueueTimeoutMs, document.FirstPcmTimeoutEarconId);

        ValidateDocumentContent(errors, document, resourceCatalog, promptCatalog, wakeWordCatalog, wakeWordDetectorCatalog);
        _catalogReferenceValidator.Validate(
            errors,
            document.ResourceCatalog.ResourceModelId,
            resourceCatalog.ResourceModelId,
            document.ResourceCatalog.CatalogVersion,
            resourceCatalog.CatalogVersion,
            "$.resourceCatalog.resourceModelId",
            "$.resourceCatalog.catalogVersion",
            "resourceModelId",
            "resource catalog version");
        _catalogReferenceValidator.Validate(
            errors,
            document.PromptCatalog.PromptCatalogModelId,
            promptCatalog.PromptCatalogModelId,
            document.PromptCatalog.CatalogVersion,
            promptCatalog.CatalogVersion,
            "$.promptCatalog.promptCatalogModelId",
            "$.promptCatalog.catalogVersion",
            "promptCatalogModelId",
            "prompt catalog version");
        _catalogReferenceValidator.Validate(
            errors,
            document.LexiconCatalog.LexiconModelId,
            lexiconCatalog.LexiconModelId,
            document.LexiconCatalog.CatalogVersion,
            lexiconCatalog.CatalogVersion,
            "$.lexiconCatalog.lexiconModelId",
            "$.lexiconCatalog.catalogVersion",
            "lexiconModelId",
            "lexicon catalog version");
        _lexiconProfileSetValidator.ValidateExactMatch(errors, document.SupportedLanguages, lexiconCatalog, "$.lexiconCatalog", document.LexiconCatalog.LexiconModelId);
        _catalogReferenceValidator.Validate(
            errors,
            document.WakeWordCatalog.WakeWordModelId,
            wakeWordCatalog.WakeWordModelId,
            document.WakeWordCatalog.CatalogVersion,
            wakeWordCatalog.CatalogVersion,
            "$.wakeWordCatalog.wakeWordModelId",
            "$.wakeWordCatalog.catalogVersion",
            "wakeWordModelId",
            "wake-word catalog version");
        _catalogReferenceValidator.Validate(
            errors,
            document.WakeWordDetectorCatalog.DetectorCatalogModelId,
            wakeWordDetectorCatalog.DetectorCatalogModelId,
            document.WakeWordDetectorCatalog.CatalogVersion,
            wakeWordDetectorCatalog.CatalogVersion,
            "$.wakeWordDetectorCatalog.detectorCatalogModelId",
            "$.wakeWordDetectorCatalog.catalogVersion",
            "detectorCatalogModelId",
            "wake-word detector catalog version");
        ValidateWakeWordProfileCoverage(errors, document, wakeWordCatalog);

        return new PreflightResult(errors, []);
    }

    private static void ValidateDocumentContent(
        List<PreflightIssue> errors,
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog,
        GenericPromptCatalog promptCatalog,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        VoiceLanguageProfilePreflightValidation.ValidateDefaultLanguage(
            errors,
            document.SupportedLanguages,
            document.DefaultLanguage,
            "$.defaultLanguage",
            document.VoiceInterfaceId);
        VoiceResourceCatalogPreflightValidation.ValidateResources(
            errors,
            document.SupportedLanguages,
            document.StrictLocalization,
            resourceCatalog.Resources,
            "$.resourceCatalog.resources");
        GenericPromptCatalogPreflightValidation.ValidatePromptCatalog(
            errors,
            document.SupportedLanguages,
            document.DefaultLanguage,
            document.StrictLocalization,
            promptCatalog,
            "$.promptCatalog.promptsByLanguage");
        WakeWordCatalogProfilePreflightValidation.ValidateCatalog(
            errors,
            document.SupportedLanguages,
            wakeWordCatalog,
            "$.wakeWordCatalog");
        WakeWordDetectorCatalogReferencePreflightValidation.ValidateDetectorTargetsWakeWordCatalog(
            errors,
            wakeWordCatalog,
            wakeWordDetectorCatalog,
            "$.wakeWordDetectorCatalog");
    }

    private static void ValidateWakeWordProfileCoverage(
        List<PreflightIssue> errors,
        VoiceInterfaceDocument document,
        WakeWordCatalog wakeWordCatalog)
    {
        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfilesExactMatch(
            errors,
            document.SupportedLanguages,
            wakeWordCatalog.ActivationProfilesByLanguage.Select(profile => profile.LanguageCode).ToArray(),
            "$.wakeWordCatalog.activationProfilesByLanguage",
            document.WakeWordCatalog.WakeWordModelId,
            "wake-word profile");
    }
}
