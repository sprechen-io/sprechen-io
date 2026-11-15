namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Normalisiert eine vorhandene Sprachauswahl oder liefert die Modellvorgabe, damit Übergänge auch
/// ohne verwendbaren Auswahlwert eine Sprache erhalten.
/// </summary>
internal static class SpeechAssistantSelectedIsoCodeResolver
{
    public static string Resolve(string? selectedIsoCode)
    {
        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(selectedIsoCode);
        if (!string.IsNullOrWhiteSpace(normalizedIsoCode))
        {
            return normalizedIsoCode;
        }

        return VoiceInterfaceModelCatalog.DefaultLanguage;
    }
}
