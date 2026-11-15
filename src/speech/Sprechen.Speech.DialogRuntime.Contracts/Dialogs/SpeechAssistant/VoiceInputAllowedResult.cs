namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Frage, ob Spracheingabe erlaubt ist (oder ob eine stille Eingabeoption angeboten
/// werden soll) für eine Variable im aktiven Formulardialog.
/// </summary>
/// <param name="Allowed">Ob Spracheingabe erlaubt ist (oder die stille Eingabeoption angeboten werden soll).</param>
/// <param name="SilentInputFallback">Die stille Eingabe-Fallback-Kennung, aufgelöst aus den Datenschutzregeln der Variable.</param>
public sealed record VoiceInputAllowedResult(bool Allowed, string SilentInputFallback);
