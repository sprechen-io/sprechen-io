namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die Übertragungswerte der Kommandosignalklassen für Klassifikationsergebnisse
/// und Telemetriedaten gemeinsam bereit.
/// </summary>
public static class CommandSignalClass
{
    public const string Clear = "clear";
    public const string Ambiguous = "ambiguous";
    public const string NoCommand = "no_command";
}
