namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert die für Katalogauswahl und Startübergabe benötigten Anwendungsdaten. Anzeigename,
/// Beschreibung und Tags dienen dem Vergleich. Formularzahl und Standardformular unterstützen die
/// anschließende Navigation.
/// </summary>
internal sealed record ApplicationInfo(
    string Id,
    string DisplayName,
    string? Description,
    IReadOnlyList<string> Tags,
    int FormCount = 1,
    string? DefaultFormId = null);


