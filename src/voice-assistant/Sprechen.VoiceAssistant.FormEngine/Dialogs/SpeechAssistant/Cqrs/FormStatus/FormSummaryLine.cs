namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Verbindet eine Feldkennung mit Bezeichnung und Wert für eine Zeile der Zusammenfassung.
/// </summary>
internal sealed record FormSummaryLine(string FieldId, string Label, string Value);


