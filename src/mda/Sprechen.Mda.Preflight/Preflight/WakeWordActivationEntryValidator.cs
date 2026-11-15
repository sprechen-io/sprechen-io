using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Einleitungswörter, Namen und zusammengesetzte Aktivierungsformen
/// auf leere Vergleichsformen und Duplikate sowie auf mindestens eine angebotene Aktivierungsform.
/// </summary>
internal sealed class WakeWordActivationEntryValidator
{
    private readonly WakeWordActivationFormNormalizer _normalizer = new();
    private readonly WakeWordActivationFormComposer _composer = new();

    public void Validate(
        List<PreflightIssue> errors,
        WakeWordCatalog catalog,
        string basePath,
        string? catalogId)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);

        ValidateLeadInSet(errors, catalog, basePath, catalogId);

        var profiles = catalog.ActivationProfilesByLanguage;
        for (var profileIndex = 0; profileIndex < profiles.Count; profileIndex++)
        {
            ValidateProfile(errors, catalog, profiles[profileIndex], profileIndex, basePath, catalogId);
        }
    }

    private void ValidateLeadInSet(
        List<PreflightIssue> errors,
        WakeWordCatalog catalog,
        string basePath,
        string? catalogId)
    {
        var leadIns = catalog.CommandAnchorLeadIns;
        var leadInsPath = $"{basePath}.commandAnchorLeadIns";

        if (leadIns.Count == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: "Wake-word command-anchor lead-in set must declare at least one lead-in.",
                Path: leadInsPath,
                Id: catalogId));
            return;
        }

        var activationRoleCount = 0;
        for (var index = 0; index < leadIns.Count; index++)
        {
            var leadIn = leadIns[index];
            if (_normalizer.NormalizeTokens(leadIn.LeadIn).Length == 0)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorLanguageInvariantBroken,
                    Message: "Wake-word command-anchor lead-in must normalize to at least one token.",
                    Path: $"{leadInsPath}[{index}].leadIn",
                    Id: catalogId));
            }

            if (string.Equals(leadIn.Role, CommandAnchorLeadInRoles.Activation, StringComparison.OrdinalIgnoreCase))
            {
                activationRoleCount++;
            }
        }

        if (activationRoleCount == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: "Wake-word command-anchor lead-in set must declare at least one activation-role lead-in.",
                Path: leadInsPath,
                Id: catalogId));
        }
    }

    private void ValidateProfile(
        List<PreflightIssue> errors,
        WakeWordCatalog catalog,
        WakeWordLanguageProfile profile,
        int profileIndex,
        string basePath,
        string? catalogId)
    {
        var profilePath = $"{basePath}.activationProfilesByLanguage[{profileIndex}]";

        if (_normalizer.NormalizeTokens(profile.WakeWordName).Length == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: "Wake-word name must normalize to at least one token.",
                Path: $"{profilePath}.wakeWordName",
                Id: catalogId));
        }

        var activationForms = _composer.ComposeActivationForms(catalog, profile);
        var recognitionOnlyForms = _composer.ComposeRecognitionOnlyActivationForms(catalog, profile);

        ValidateEntryList(
            errors,
            activationForms,
            forbiddenKeys: new HashSet<string>(StringComparer.Ordinal),
            $"{profilePath}.activationForms",
            catalogId,
            "activation form");

        ValidateEntryList(
            errors,
            recognitionOnlyForms,
            GetActivationKeys(activationForms),
            $"{profilePath}.recognitionOnlyActivationForms",
            catalogId,
            "recognition-only activation form");
    }

    private HashSet<string> GetActivationKeys(IReadOnlyList<string> activationForms)
    {
        return activationForms
            .Select(_normalizer.NormalizeValueKey)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private void ValidateEntryList(
        List<PreflightIssue> errors,
        IReadOnlyList<string> entries,
        IReadOnlySet<string> forbiddenKeys,
        string path,
        string? catalogId,
        string entryKind)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(forbiddenKeys);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryKind);

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < entries.Count; index++)
        {
            ValidateEntry(errors, entries[index], seenKeys, forbiddenKeys, $"{path}[{index}]", catalogId, entryKind);
        }
    }

    private void ValidateEntry(
        List<PreflightIssue> errors,
        string entryValue,
        HashSet<string> seenKeys,
        IReadOnlySet<string> forbiddenKeys,
        string path,
        string? catalogId,
        string entryKind)
    {
        var normalizedTokens = _normalizer.NormalizeTokens(entryValue);
        if (normalizedTokens.Length == 0)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: $"Wake-word {entryKind} must normalize to at least one token.",
                Path: path,
                Id: catalogId));
            return;
        }

        var normalizedKey = string.Join(" ", normalizedTokens);
        if (!seenKeys.Add(normalizedKey))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: $"Wake-word {entryKind} duplicates another entry after normalization.",
                Path: path,
                Id: catalogId));
            return;
        }

        if (forbiddenKeys.Contains(normalizedKey))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: $"Wake-word {entryKind} duplicates a canonical activation form after normalization.",
                Path: path,
                Id: catalogId));
        }
    }
}
