namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsame Prüfungen für Standardsprache und Sprachprofilabdeckung
/// sowie den exakten Vergleich von Sprachlisten.
/// </summary>
internal static class VoiceLanguageProfilePreflightValidation
{
    internal static void ValidateDefaultLanguage(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        string defaultLanguage,
        string path,
        string id)
    {
        if (supportedLanguages.Contains(defaultLanguage, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLanguageInvariantBroken,
            Message: $"defaultLanguage '{defaultLanguage}' must be contained in supportedLanguages.",
            Path: path,
            Id: id));
    }

    internal static void ValidateLanguageProfiles(
        List<PreflightIssue> errors,
        IReadOnlyList<string> supportedLanguages,
        IReadOnlyList<string> languagesFromProfiles,
        string path,
        string profileKind)
    {
        var languagesSeen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < languagesFromProfiles.Count; index++)
        {
            var language = languagesFromProfiles[index];
            if (!languagesSeen.Add(language))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorIdDuplicate,
                    Message: $"Duplicate {profileKind} languageCode '{language}'.",
                    Path: $"{path}[{index}].languageCode",
                    Id: language));
            }
        }

        foreach (var language in supportedLanguages)
        {
            if (languagesSeen.Contains(language))
            {
                continue;
            }

            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLocalizationMissing,
                Message: $"Missing {profileKind} for language '{language}'.",
                Path: path,
                Id: language));
        }
    }

    internal static void ValidateLanguageProfilesExactMatch(
        List<PreflightIssue> errors,
        IReadOnlyList<string> expectedLanguages,
        IReadOnlyList<string> actualLanguages,
        string path,
        string id,
        string profileKind)
    {
        var expected = expectedLanguages.OrderBy(language => language, StringComparer.Ordinal).ToArray();
        var actual = actualLanguages.OrderBy(language => language, StringComparer.Ordinal).ToArray();

        if (expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorLanguageInvariantBroken,
            Message: $"{profileKind} languages must match the owning voice-interface supportedLanguages exactly. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
            Path: path,
            Id: id));
    }
}
