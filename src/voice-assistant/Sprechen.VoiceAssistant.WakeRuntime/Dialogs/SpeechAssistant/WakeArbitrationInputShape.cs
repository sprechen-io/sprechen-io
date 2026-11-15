namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält fest, welche Erkennungsergebnisse oder Ersatzgründe vorliegen,
/// damit Konfliktprüfung und Auswahl des Verarbeitungswegs dieselben Angaben nutzen.
/// </summary>
internal readonly record struct WakeArbitrationInputShape(
    bool HasFallback,
    bool HasAcoustic,
    bool HasLexical,
    bool HasStitch)
{
    public bool HasDetectionInputs => HasAcoustic || HasLexical || HasStitch;
}
