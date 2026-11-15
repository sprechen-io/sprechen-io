namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert Kennung, Anzeigenamen und Voraussetzungen eines Formulars für die Auswahl. Der
/// Dialog kann damit Kandidaten benennen und fehlende Vorgänger prüfen, ohne das ganze
/// Formularmodell weiterzureichen.
/// </summary>
internal sealed record FormInfo(string Id, string DisplayName, IReadOnlyList<string> Prerequisites);


