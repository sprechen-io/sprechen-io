namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Löst ein Sprachoberflächendokument mit den referenzierten Katalogen
/// zu einem Laufzeitmodell auf.
/// </summary>
public sealed class VoiceInterfaceDocumentResolver
{
    /// <summary>
    /// Prüft Kennungen und Versionen der Katalogreferenzen und führt deren Inhalte
    /// im Laufzeitmodell zusammen.
    /// </summary>
    public VoiceInterface Resolve(
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog,
        GenericPromptCatalog promptCatalog,
        VoiceLexiconCatalog lexiconCatalog,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(resourceCatalog);
        ArgumentNullException.ThrowIfNull(promptCatalog);
        ArgumentNullException.ThrowIfNull(lexiconCatalog);
        ArgumentNullException.ThrowIfNull(wakeWordCatalog);
        ArgumentNullException.ThrowIfNull(wakeWordDetectorCatalog);

        ValidateReferences(document, resourceCatalog, promptCatalog, lexiconCatalog, wakeWordCatalog, wakeWordDetectorCatalog);

        return new VoiceInterface
        {
            SchemaVersion = document.SchemaVersion,
            CatalogVersion = document.CatalogVersion,
            VoiceInterfaceId = document.VoiceInterfaceId,
            SupportedLanguages = [.. document.SupportedLanguages],
            DefaultLanguage = document.DefaultLanguage,
            StrictLocalization = document.StrictLocalization,
            SessionLanguageOfferEnabled = document.SessionLanguageOfferEnabled,
            FirstPcmEnqueueTimeoutMs = document.FirstPcmEnqueueTimeoutMs,
            FirstPcmTimeoutEarconId = document.FirstPcmTimeoutEarconId,
            Resources = [.. resourceCatalog.Resources],
            PromptCatalog = promptCatalog,
            YesNoProfiles = [.. lexiconCatalog.YesNoProfiles],
            PolitenessProfiles = [.. lexiconCatalog.PolitenessProfiles],
            WakeWordCatalog = wakeWordCatalog,
            WakeWordDetectorCatalog = wakeWordDetectorCatalog
        };
    }

    private static void ValidateReferences(
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog,
        GenericPromptCatalog promptCatalog,
        VoiceLexiconCatalog lexiconCatalog,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        ValidateResourceCatalogReference(document, resourceCatalog);
        ValidatePromptCatalogReference(document, promptCatalog);
        ValidateLexiconCatalogReference(document, lexiconCatalog);
        ValidateWakeWordCatalogReference(document, wakeWordCatalog);
        ValidateWakeWordDetectorCatalogReference(document, wakeWordCatalog, wakeWordDetectorCatalog);
    }

    private static void ValidateResourceCatalogReference(
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog)
    {
        if (!string.Equals(document.ResourceCatalog.ResourceModelId, resourceCatalog.ResourceModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references resourceModelId '{document.ResourceCatalog.ResourceModelId}', but loaded catalog is '{resourceCatalog.ResourceModelId}'.");
        }

        if (!string.Equals(document.ResourceCatalog.CatalogVersion, resourceCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references resource catalog version '{document.ResourceCatalog.CatalogVersion}', but loaded catalog is '{resourceCatalog.CatalogVersion}'.");
        }
    }

    private static void ValidatePromptCatalogReference(
        VoiceInterfaceDocument document,
        GenericPromptCatalog promptCatalog)
    {
        if (!string.Equals(document.PromptCatalog.PromptCatalogModelId, promptCatalog.PromptCatalogModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references promptCatalogModelId '{document.PromptCatalog.PromptCatalogModelId}', but loaded catalog is '{promptCatalog.PromptCatalogModelId}'.");
        }

        if (!string.Equals(document.PromptCatalog.CatalogVersion, promptCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references prompt catalog version '{document.PromptCatalog.CatalogVersion}', but loaded catalog is '{promptCatalog.CatalogVersion}'.");
        }
    }

    private static void ValidateLexiconCatalogReference(
        VoiceInterfaceDocument document,
        VoiceLexiconCatalog lexiconCatalog)
    {
        if (!string.Equals(document.LexiconCatalog.LexiconModelId, lexiconCatalog.LexiconModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references lexiconModelId '{document.LexiconCatalog.LexiconModelId}', but loaded catalog is '{lexiconCatalog.LexiconModelId}'.");
        }

        if (!string.Equals(document.LexiconCatalog.CatalogVersion, lexiconCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references lexicon catalog version '{document.LexiconCatalog.CatalogVersion}', but loaded catalog is '{lexiconCatalog.CatalogVersion}'.");
        }
    }

    private static void ValidateWakeWordCatalogReference(
        VoiceInterfaceDocument document,
        WakeWordCatalog wakeWordCatalog)
    {
        if (!string.Equals(document.WakeWordCatalog.WakeWordModelId, wakeWordCatalog.WakeWordModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references wakeWordModelId '{document.WakeWordCatalog.WakeWordModelId}', but loaded catalog is '{wakeWordCatalog.WakeWordModelId}'.");
        }

        if (!string.Equals(document.WakeWordCatalog.CatalogVersion, wakeWordCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references wake-word catalog version '{document.WakeWordCatalog.CatalogVersion}', but loaded catalog is '{wakeWordCatalog.CatalogVersion}'.");
        }
    }

    private static void ValidateWakeWordDetectorCatalogReference(
        VoiceInterfaceDocument document,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        if (!string.Equals(document.WakeWordDetectorCatalog.DetectorCatalogModelId, wakeWordDetectorCatalog.DetectorCatalogModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references detectorCatalogModelId '{document.WakeWordDetectorCatalog.DetectorCatalogModelId}', but loaded catalog is '{wakeWordDetectorCatalog.DetectorCatalogModelId}'.");
        }

        if (!string.Equals(document.WakeWordDetectorCatalog.CatalogVersion, wakeWordDetectorCatalog.CatalogVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"VoiceInterface references wake-word detector catalog version '{document.WakeWordDetectorCatalog.CatalogVersion}', but loaded catalog is '{wakeWordDetectorCatalog.CatalogVersion}'.");
        }

        if (!string.Equals(wakeWordDetectorCatalog.WakeWordModelId, wakeWordCatalog.WakeWordModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Wake-word detector catalog '{wakeWordDetectorCatalog.DetectorCatalogModelId}' targets wakeWordModelId '{wakeWordDetectorCatalog.WakeWordModelId}', but the loaded wake-word catalog is '{wakeWordCatalog.WakeWordModelId}'.");
        }
    }
}
