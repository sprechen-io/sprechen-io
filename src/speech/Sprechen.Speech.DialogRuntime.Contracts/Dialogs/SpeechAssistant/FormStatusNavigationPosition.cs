namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt die Position in Anwendung, Formular und Feldgruppe für Orientierungsangaben innerhalb der Statusansage.
/// </summary>
public sealed record FormStatusNavigationPosition(
    string? CurrentFormId,
    int? CurrentFormIndex,
    int? TotalForms,
    string CurrentFieldGroupId,
    int CurrentFieldGroupIndex,
    int TotalFieldGroups,
    string? CurrentFieldId);
