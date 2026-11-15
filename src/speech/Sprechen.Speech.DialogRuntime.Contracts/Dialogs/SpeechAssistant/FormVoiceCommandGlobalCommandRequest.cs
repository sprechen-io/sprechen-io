namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Befehl, Sprache und optionales Argument als gemeinsame Eingabe der globalen Befehlshandler.
/// </summary>
public sealed record FormVoiceCommandGlobalCommandRequest(
    string CommandId,
    string IsoCode,
    string? Argument);
