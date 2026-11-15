using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die Prüfung von Sprachausgabeparametern, Texten, Sprachprofilen
/// und Aktivierungswortkatalogen der aufgelösten Sprachoberfläche.
/// </summary>
internal sealed class VoiceInterfacePreflightValidator
{
    private readonly VoiceLexiconProfileSetPreflightValidator _lexiconProfileSetValidator = new();

    public PreflightResult Validate(VoiceInterface model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var errors = new List<PreflightIssue>();
        var warnings = new List<PreflightIssue>();

        ValidateSpeechOutput(errors, model.FirstPcmEnqueueTimeoutMs, model.FirstPcmTimeoutEarconId);

        VoiceLanguageProfilePreflightValidation.ValidateDefaultLanguage(
            errors,
            model.SupportedLanguages,
            model.DefaultLanguage,
            "$.defaultLanguage",
            model.VoiceInterfaceId);
        VoiceResourceCatalogPreflightValidation.ValidateResources(
            errors,
            model.SupportedLanguages,
            model.StrictLocalization,
            model.Resources,
            "$.resources");
        GenericPromptCatalogPreflightValidation.ValidatePromptCatalog(
            errors,
            model.SupportedLanguages,
            model.DefaultLanguage,
            model.StrictLocalization,
            model.PromptCatalog,
            "$.promptCatalog.promptsByLanguage");
        _lexiconProfileSetValidator.ValidateExactMatch(errors, model.SupportedLanguages, CreateLexiconCatalog(model), "$", model.VoiceInterfaceId);
        WakeWordCatalogProfilePreflightValidation.ValidateCatalog(
            errors,
            model.SupportedLanguages,
            model.WakeWordCatalog,
            "$.wakeWordCatalog");
        WakeWordDetectorCatalogReferencePreflightValidation.ValidateDetectorTargetsWakeWordCatalog(
            errors,
            model.WakeWordCatalog,
            model.WakeWordDetectorCatalog,
            "$.wakeWordDetectorCatalog");

        return new PreflightResult(errors, warnings);
    }

    internal void ValidateSpeechOutput(List<PreflightIssue> errors, int timeoutMs, string? earconId)
    {
        if (timeoutMs is < 1000 or > 60000)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorValueOutOfRange,
                Message: "First PCM enqueue timeout must be between 1000 and 60000 milliseconds.",
                Path: "$.firstPcmEnqueueTimeoutMs"));
        }

        if (!string.Equals(earconId, "ValidationAsrFail", StringComparison.Ordinal))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "First PCM timeout requires the finite negative cue ValidationAsrFail.",
                Path: "$.firstPcmTimeoutEarconId"));
        }
    }

    private static VoiceLexiconCatalog CreateLexiconCatalog(VoiceInterface model)
    {
        return new VoiceLexiconCatalog
        {
            LexiconModelId = model.VoiceInterfaceId,
            YesNoProfiles = model.YesNoProfiles,
            PolitenessProfiles = model.PolitenessProfiles
        };
    }
}
