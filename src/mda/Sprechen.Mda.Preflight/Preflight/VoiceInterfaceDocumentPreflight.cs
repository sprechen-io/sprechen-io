using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft ein Sprachoberflächendokument zusammen mit seinen referenzierten
/// Text-, Ansagen-, Lexikon- und Aktivierungswortkatalogen.
/// </summary>
public static class VoiceInterfaceDocumentPreflight
{
    /// <summary>
    /// Prüft Dokumentregeln und die Konsistenz der referenzierten Kataloge.
    /// </summary>
    public static PreflightResult Validate(
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

        return new VoiceInterfaceDocumentPreflightValidator().Validate(
            document,
            resourceCatalog,
            promptCatalog,
            lexiconCatalog,
            wakeWordCatalog,
            wakeWordDetectorCatalog);
    }
}
