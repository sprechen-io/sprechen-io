namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Verarbeitungswege für lexikalischen Ersatz, direkte Aktivierung und die
/// Behandlung nicht aktivierender oder zusammengesetzter Äußerungen.
/// </summary>
internal enum WakeArbitrationRoute
{
    LexicalFallback,
    AcousticActivation,
    LexicalActivation,
    LexicalMissOrStitch,
}
