namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt abgeschlossene und insgesamt betrachtete Einheiten als Zählerpaar für eine Fortschrittsansage.
/// </summary>
public sealed record FormStatusProgressDimension(
    int Completed,
    int Total);
