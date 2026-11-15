using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt Sprache, Aktivierungsformen und Detektorprofil mit dem Ersatzgrund, damit die Erkennung eine konsistente Konfiguration erhält.
/// </summary>
public sealed record AcousticWakeWordDetectorConfiguration(
    string LanguageCode,
    WakeWordInventory WakeWordInventory,
    WakeWordDetectorProfile DetectorProfile,
    bool RequiresLexicalFallback,
    string LexicalFallbackReasonCode)
{
    public bool AllowsAcousticDetection =>
        !RequiresLexicalFallback
        && DetectorProfile.OperatingMode == WakeWordDetectorOperatingMode.AcousticPreferred;
}
