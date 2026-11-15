using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft aufgelöste Auswahlnamen je Sprache und Anrede auf leere Vergleichsschlüssel,
/// doppelte Namen und Überschneidungen mit Auswahlkommandos. Ist ein Aktivierungswortkatalog
/// angegeben, werden auch Überschneidungen mit dessen Aktivierungsformen geprüft.
/// </summary>
internal sealed class CatalogSelectionVocabularyPreflightValidator
{
    private readonly WakeWordActivationFormComposer _wakeWordActivationFormComposer = new();
    private readonly CatalogSelectionTextNormalizer _normalizer = new();
    private readonly CatalogSelectionNonNameIntentClassifier _classifier;

    public CatalogSelectionVocabularyPreflightValidator()
    {
        _classifier = new CatalogSelectionNonNameIntentClassifier(_normalizer);
    }

    public PreflightResult Validate(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return ValidateCore(catalog, wakeWordCatalog: null);
    }

    public PreflightResult Validate(FormsCatalog catalog, WakeWordCatalog wakeWordCatalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(wakeWordCatalog);

        return ValidateCore(catalog, wakeWordCatalog);
    }

    private PreflightResult ValidateCore(FormsCatalog catalog, WakeWordCatalog? wakeWordCatalog)
    {
        var errors = new List<PreflightIssue>();
        var applicationNames = new Dictionary<(string Language, PolitenessLevel Politeness, string Key), string>();

        for (var applicationIndex = 0; applicationIndex < catalog.Applications.Count; applicationIndex++)
        {
            var application = catalog.Applications[applicationIndex];
            var applicationPath = $"$.applications[{applicationIndex}].displayName";
            ValidateNames(
                errors,
                catalog,
                wakeWordCatalog,
                application.DisplayName,
                application.SupportedLanguages,
                applicationPath,
                application.ApplicationId,
                $"Application '{application.ApplicationId}'",
                applicationNames);

            var formNames = new Dictionary<(string Language, PolitenessLevel Politeness, string Key), string>();
            for (var formIndex = 0; formIndex < application.Forms.Count; formIndex++)
            {
                var form = application.Forms[formIndex];
                var formId = form.FormId == default ? string.Empty : form.FormId.Value;
                var formPath = $"$.applications[{applicationIndex}].forms[{formIndex}].displayName";
                ValidateNames(
                    errors,
                    catalog,
                    wakeWordCatalog,
                    form.DisplayName,
                    application.SupportedLanguages,
                    formPath,
                    formId,
                    $"Form '{formId}' in application '{application.ApplicationId}'",
                    formNames);
            }
        }

        return new PreflightResult(errors, []);
    }

    private void ValidateNames(
        List<PreflightIssue> errors,
        FormsCatalog catalog,
        WakeWordCatalog? wakeWordCatalog,
        IReadOnlyDictionary<string, string> displayName,
        IReadOnlyList<string> supportedLanguages,
        string displayNamePath,
        string id,
        string entityDescription,
        Dictionary<(string Language, PolitenessLevel Politeness, string Key), string> namesByScope)
    {
        foreach (var language in supportedLanguages
                     .Where(item => !string.IsNullOrWhiteSpace(item))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var politenessLevel in Enum.GetValues<PolitenessLevel>())
            {
                if (!TryResolveDisplayName(displayName, language, politenessLevel, out var resolvedName, out var resolvedKey))
                {
                    continue;
                }

                var path = $"{displayNamePath}['{resolvedKey}']";
                var register = ToWireValue(politenessLevel);
                var compactKey = _normalizer.NormalizeForCompactMatching(resolvedName);
                if (compactKey.Length == 0)
                {
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorSelectionNameEmpty,
                        Message: $"{entityDescription} selection name for language '{language}', register '{register}', resolved key '{resolvedKey}' has empty compact-normalized key ''.",
                        Path: path,
                        Id: id));
                }
                else
                {
                    var scope = (language.Trim().ToLowerInvariant(), politenessLevel, compactKey);
                    if (namesByScope.TryGetValue(scope, out var firstPath))
                    {
                        errors.Add(new PreflightIssue(
                            Code: PreflightCodes.ErrorSelectionNameDuplicate,
                            Message: $"{entityDescription} selection name for language '{language}', register '{register}', normalized key '{compactKey}' duplicates the first selection name at '{firstPath}'.",
                            Path: path,
                            Id: id));
                    }
                    else
                    {
                        namesByScope[scope] = path;
                    }
                }

                var intentId = _classifier.ClassifyNonNameIntent(catalog, resolvedName, language, politenessLevel);
                if (intentId is not null)
                {
                    var phraseKey = _normalizer.NormalizeForIntentMatching(resolvedName, language);
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorSelectionNameIntentCollision,
                        Message: $"{entityDescription} selection name for language '{language}', register '{register}', normalized key '{phraseKey}' collides with non-name intent '{intentId}'.",
                        Path: path,
                        Id: id));
                }

                if (wakeWordCatalog is not null
                    && TryFindWakeCollision(
                        wakeWordCatalog,
                        resolvedName,
                        language,
                        out var wakeKey,
                        out var wakeRole,
                        out var leadInPath,
                        out var wakeWordNamePath))
                {
                    errors.Add(new PreflightIssue(
                        Code: PreflightCodes.ErrorSelectionNameWakeCollision,
                        Message: $"{entityDescription} selection name for language '{language}', register '{register}', normalized key '{wakeKey}' collides with wake-word role '{wakeRole}' composed from '{leadInPath}' and '{wakeWordNamePath}'.",
                        Path: path,
                        Id: id));
                }
            }
        }
    }

    private static bool TryResolveDisplayName(
        IReadOnlyDictionary<string, string> displayName,
        string language,
        PolitenessLevel politenessLevel,
        out string resolvedName,
        out string resolvedKey)
    {
        var normalizedLanguage = language.Trim();
        var variantKey = $"{normalizedLanguage}@{ToWireValue(politenessLevel)}";
        if (displayName.TryGetValue(variantKey, out var variant) && variant is not null)
        {
            resolvedName = variant;
            resolvedKey = variantKey;
            return true;
        }

        if (displayName.TryGetValue(normalizedLanguage, out var baseValue) && baseValue is not null)
        {
            resolvedName = baseValue;
            resolvedKey = normalizedLanguage;
            return true;
        }

        resolvedName = string.Empty;
        resolvedKey = string.Empty;
        return false;
    }

    private bool TryFindWakeCollision(
        WakeWordCatalog wakeWordCatalog,
        string resolvedName,
        string language,
        out string normalizedKey,
        out string role,
        out string leadInPath,
        out string wakeWordNamePath)
    {
        normalizedKey = _normalizer.NormalizeForPhraseMatching(resolvedName);
        role = string.Empty;
        leadInPath = string.Empty;
        wakeWordNamePath = string.Empty;

        for (var profileIndex = 0; profileIndex < wakeWordCatalog.ActivationProfilesByLanguage.Count; profileIndex++)
        {
            var profile = wakeWordCatalog.ActivationProfilesByLanguage[profileIndex];
            if (!string.Equals(profile.LanguageCode?.Trim(), language.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryFindWakeCollisionForRole(
                    wakeWordCatalog,
                    profileIndex,
                    normalizedKey,
                    CommandAnchorLeadInRoles.Activation,
                    _wakeWordActivationFormComposer.ComposeActivationForms(wakeWordCatalog, profile),
                    out leadInPath,
                    out wakeWordNamePath))
            {
                role = CommandAnchorLeadInRoles.Activation;
                return true;
            }

            if (TryFindWakeCollisionForRole(
                    wakeWordCatalog,
                    profileIndex,
                    normalizedKey,
                    CommandAnchorLeadInRoles.RecognitionOnly,
                    _wakeWordActivationFormComposer.ComposeRecognitionOnlyActivationForms(wakeWordCatalog, profile),
                    out leadInPath,
                    out wakeWordNamePath))
            {
                role = CommandAnchorLeadInRoles.RecognitionOnly;
                return true;
            }
        }

        return false;
    }

    private bool TryFindWakeCollisionForRole(
        WakeWordCatalog wakeWordCatalog,
        int profileIndex,
        string normalizedName,
        string expectedRole,
        IReadOnlyList<string> composedForms,
        out string leadInPath,
        out string wakeWordNamePath)
    {
        var composedIndex = 0;
        for (var leadInIndex = 0; leadInIndex < wakeWordCatalog.CommandAnchorLeadIns.Count; leadInIndex++)
        {
            var leadIn = wakeWordCatalog.CommandAnchorLeadIns[leadInIndex];
            if (leadIn is null || !string.Equals(leadIn.Role, expectedRole, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (composedIndex >= composedForms.Count)
            {
                break;
            }

            var normalizedForm = _normalizer.NormalizeForPhraseMatching(composedForms[composedIndex]);
            composedIndex++;
            if (!string.Equals(normalizedName, normalizedForm, StringComparison.Ordinal))
            {
                continue;
            }

            leadInPath = $"$.commandAnchorLeadIns[{leadInIndex}].leadIn";
            wakeWordNamePath = $"$.activationProfilesByLanguage[{profileIndex}].wakeWordName";
            return true;
        }

        leadInPath = string.Empty;
        wakeWordNamePath = string.Empty;
        return false;
    }

    private static string ToWireValue(PolitenessLevel politenessLevel)
    {
        return politenessLevel switch
        {
            PolitenessLevel.Formal => "formal",
            PolitenessLevel.Informal => "informal",
            _ => throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.")
        };
    }
}
