namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Erstellt den aufgelösten Anwendungskatalog, prüft Dialog- und Profilverweise
/// und kombiniert anwendungsspezifische mit globalen Dialogeinstellungen.
/// </summary>
public sealed class FormsCatalogDocumentResolver
{
    public FormsCatalog Resolve(
        FormsCatalogDocument document,
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selectionDialog);
        ArgumentNullException.ThrowIfNull(profilesByKey);

        if (!string.Equals(document.SelectionDialog.SelectionDialogId, selectionDialog.SelectionDialogId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormsCatalog references selectionDialogId '{document.SelectionDialog.SelectionDialogId}', but loaded model is '{selectionDialog.SelectionDialogId}'.");
        }

        if (!string.Equals(document.SelectionDialog.ModelVersion, selectionDialog.ModelVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FormsCatalog references selection-dialog model version '{document.SelectionDialog.ModelVersion}', but loaded model is '{selectionDialog.ModelVersion}'.");
        }

        var catalogProfile = ResolveProfileReference(document.SelectionDialogProfile, profilesByKey);
        var catalogSettings = ToVoiceDialogSettings(catalogProfile);
        var resolvedApplications = new List<ApplicationDescriptor>(document.Applications.Count);

        foreach (var application in document.Applications)
        {
            var applicationProfile = ResolveProfileReference(application.SelectionDialogProfile, profilesByKey);
            var applicationSettings = ToVoiceDialogSettings(applicationProfile);
            var mergedSettings = applicationSettings?.MergeWith(catalogSettings) ?? catalogSettings;

            resolvedApplications.Add(new ApplicationDescriptor
            {
                ApplicationId = application.ApplicationId,
                DisplayName = CloneRequiredLocalizedText(application.DisplayName),
                Description = CloneRequiredLocalizedText(application.Description),
                Tags = application.Tags is null ? null : [.. application.Tags],
                SupportedLanguages = [.. application.SupportedLanguages],
                DefaultLanguage = application.DefaultLanguage,
                Icon = application.Icon,
                IsStudyApplication = application.IsStudyApplication,
                VoiceDialogSettings = mergedSettings,
                Forms = [.. application.Forms]
            });
        }

        return new FormsCatalog
        {
            SchemaVersion = document.SchemaVersion,
            CatalogVersion = document.CatalogVersion,
            GeneratedAt = document.GeneratedAt,
            VoiceDialogSettings = catalogSettings,
            SelectionDialog = CloneSelectionDialog(selectionDialog),
            Applications = resolvedApplications
        };
    }

    private static ApplicationSelectionDialogProfile? ResolveProfileReference(
        ApplicationSelectionDialogProfileReference? reference,
        IReadOnlyDictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> profilesByKey)
    {
        if (reference is null)
        {
            return null;
        }

        if (!profilesByKey.TryGetValue(new ApplicationSelectionDialogProfileKey(reference.ProfileId, reference.ProfileVersion), out var profile))
        {
            throw new InvalidOperationException(
                $"FormsCatalog references missing selection-dialog profile '{reference.ProfileId}/{reference.ProfileVersion}'.");
        }

        return profile;
    }

    private static VoiceDialogSettings? ToVoiceDialogSettings(ApplicationSelectionDialogProfile? profile)
    {
        if (profile is null)
        {
            return null;
        }

        return new VoiceDialogSettings
        {
            NoInputTimeoutMs = profile.NoInputTimeoutMs,
            MaxRetries = profile.MaxRetries,
            ConfirmationPolicy = profile.ConfirmationPolicy,
            SttMinConfidenceToAccept = profile.SttMinConfidenceToAccept,
            SttMinConfidenceToConfirm = profile.SttMinConfidenceToConfirm,
            SpeechRate = profile.SpeechRate,
            PauseBetweenItemsMs = profile.PauseBetweenItemsMs,
            CancelRefractoryMs = profile.CancelRefractoryMs,
            AllowBargeIn = profile.AllowBargeIn,
            AutoAdvanceAfterCommit = profile.AutoAdvanceAfterCommit,
            PolitenessLevel = profile.PolitenessLevel,
            ProgressCalculationBasis = profile.ProgressCalculationBasis
        };
    }

    private static LocalizedText CloneRequiredLocalizedText(LocalizedText text)
    {
        var clone = new LocalizedText();
        foreach (var pair in text)
        {
            clone[pair.Key] = pair.Value;
        }

        return clone;
    }

    private static ApplicationSelectionDialog CloneSelectionDialog(ApplicationSelectionDialog selectionDialog)
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = selectionDialog.SchemaVersion,
            ModelVersion = selectionDialog.ModelVersion,
            SelectionDialogId = selectionDialog.SelectionDialogId,
            SupportedLanguages = [.. selectionDialog.SupportedLanguages],
            StrictLocalization = selectionDialog.StrictLocalization,
            DefaultLanguage = selectionDialog.DefaultLanguage,
            Prompts = [.. selectionDialog.Prompts],
            Intents = [.. selectionDialog.Intents],
            NumberWords = [.. selectionDialog.NumberWords]
        };
    }
}
