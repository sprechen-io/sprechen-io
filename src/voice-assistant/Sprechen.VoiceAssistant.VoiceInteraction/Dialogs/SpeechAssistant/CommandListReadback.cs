namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die geordneten Kategoriegruppen einer Befehlsübersicht mit Gesamtzähler
/// und Kürzungskennzeichen für die spätere Ansage zusammen.
/// </summary>
internal sealed record CommandListReadback(
    IReadOnlyList<CommandListGroup> Groups,
    int TotalCount,
    bool Truncated);
