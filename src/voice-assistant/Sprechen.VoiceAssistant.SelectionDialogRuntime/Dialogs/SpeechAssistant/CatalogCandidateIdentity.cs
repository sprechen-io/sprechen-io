namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Kennung und Anzeigenamen für den gemeinsamen Kandidatenvergleich. So kann die
/// Prüfung konkurrierender Bestätigungshinweise Anwendungen und Formulare gleich behandeln.
/// </summary>
internal sealed record CatalogCandidateIdentity(string Id, string DisplayName);
