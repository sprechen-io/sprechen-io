using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Sprachprofilmengen auf Duplikate und auf Übereinstimmung
/// mit einer erwarteten Sprachliste oder miteinander.
/// </summary>
internal sealed class VoiceLexiconProfileSetPreflightValidator
{
    public void ValidateExactMatch(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        VoiceLexiconCatalog catalog,
        string path,
        string id)
    {
        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfilesExactMatch(
            errors,
            supportedLanguages,
            catalog.YesNoProfiles.Select(profile => profile.LanguageCode).ToArray(),
            $"{path}.yesNoProfiles",
            id,
            "yes/no profile");
        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfilesExactMatch(
            errors,
            supportedLanguages,
            catalog.PolitenessProfiles.Select(profile => profile.LanguageCode).ToArray(),
            $"{path}.politenessProfiles",
            id,
            "politeness profile");
    }

    public void ValidateStandaloneCatalog(List<PreflightIssue> errors, VoiceLexiconCatalog catalog)
    {
        var yesNoLanguages = ValidateStandaloneProfiles(
            errors,
            catalog.YesNoProfiles.Select(profile => profile.LanguageCode).ToArray(),
            "$.yesNoProfiles",
            "yes/no profile");
        var politenessLanguages = ValidateStandaloneProfiles(
            errors,
            catalog.PolitenessProfiles.Select(profile => profile.LanguageCode).ToArray(),
            "$.politenessProfiles",
            "politeness profile");
        ValidateSameLanguageSet(errors, catalog, yesNoLanguages, politenessLanguages);
    }

    private static string[] ValidateStandaloneProfiles(
        List<PreflightIssue> errors,
        string[] profileLanguages,
        string path,
        string profileKind)
    {
        var distinctLanguages = profileLanguages.Distinct(StringComparer.Ordinal).ToArray();
        VoiceLanguageProfilePreflightValidation.ValidateLanguageProfiles(
            errors,
            distinctLanguages,
            profileLanguages,
            path,
            profileKind);

        return distinctLanguages.OrderBy(language => language, StringComparer.Ordinal).ToArray();
    }

    private static void ValidateSameLanguageSet(
        List<PreflightIssue> errors,
        VoiceLexiconCatalog catalog,
        string[] yesNoLanguages,
        string[] politenessLanguages)
    {
        if (yesNoLanguages.SequenceEqual(politenessLanguages, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLanguageInvariantBroken,
            Message: $"yesNoProfiles and politenessProfiles must expose the same languageCode set. yes/no [{string.Join(", ", yesNoLanguages)}], politeness [{string.Join(", ", politenessLanguages)}].",
            Path: "$",
            Id: catalog.LexiconModelId));
    }
}
