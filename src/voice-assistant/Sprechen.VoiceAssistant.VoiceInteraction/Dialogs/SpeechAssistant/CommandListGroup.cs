using Sprechen.Mda.Contracts.V1.VoiceCommands;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet eine Befehlskategorie mit ihrer zur Ausgabe gewählten Teilfolge und
/// dem angegebenen Gesamtzähler. Eine Kürzung führt dieser Datensatz selbst nicht aus.
/// </summary>
internal sealed record CommandListGroup(
    VoiceCommandCategory Category,
    IReadOnlyList<string> ShownLabels,
    int GroupTotal);
