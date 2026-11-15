namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Löst ein <see cref="VoiceInterfaceDocument"/> mit seinen referenzierten
/// Katalogen zu einem <see cref="VoiceInterface"/> auf.
/// </summary>
public static class VoiceInterfaceResolver
{
    /// <summary>
    /// Führt die angegebenen Kataloge nach Prüfung ihrer Referenzkennungen und Versionen
    /// mit dem Sprachoberflächendokument zusammen.
    /// </summary>
    public static VoiceInterface Resolve(
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

        return new VoiceInterfaceDocumentResolver()
            .Resolve(document, resourceCatalog, promptCatalog, lexiconCatalog, wakeWordCatalog, wakeWordDetectorCatalog);
    }
}

