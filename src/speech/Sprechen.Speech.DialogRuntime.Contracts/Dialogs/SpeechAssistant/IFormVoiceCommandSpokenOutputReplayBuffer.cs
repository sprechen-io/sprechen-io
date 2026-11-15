namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die zuletzt gesprochene Ausgabe für den Befehl <c>repeat</c> und eine
/// separate Ansage des aktuellen Feldzugs für die automatische Wiederholung.
/// </summary>
/// <remarks>
/// <see cref="LastSpokenOutput"/> bleibt bei Feldwechseln erhalten. Die automatische
/// Wiederholung verwendet dagegen <see cref="CurrentFieldTurnAnnouncement"/>:
/// <see cref="BeginFieldTurn"/> verwirft sie beim Wechsel des Feldschlüssels,
/// <see cref="MarkCurrentFieldTurnAnnouncementReplayed"/> markiert sie nach einmaliger
/// Wiederholung als verbraucht.
/// </remarks>
public interface IFormVoiceCommandSpokenOutputReplayBuffer
{
    void Record(string text);

    string? LastSpokenOutput { get; }

    /// <summary>
    /// Beginnt die Ansageverwaltung für einen Feldzug. Bei geändertem Feldschlüssel wird
    /// die ausstehende automatische Wiederholung verworfen, damit sie nicht später
    /// den Inhalt des vorherigen Feldes spricht.
    /// </summary>
    void BeginFieldTurn(string fieldTurnKey);

    /// <summary>
    /// Zuletzt im aktuellen Feldzug aufgezeichnete Ansage. Der Wert ist null, wenn keine
    /// Ansage vorliegt, das Feld gewechselt hat oder die Ansage bereits automatisch wiederholt wurde.
    /// </summary>
    string? CurrentFieldTurnAnnouncement { get; }

    /// <summary>
    /// Markiert die Ansage als bereits automatisch wiederholt. Weitere Stille führt
    /// dann zur vorgesehenen Nachfrage oder Hilfe statt zu derselben Ansage.
    /// </summary>
    void MarkCurrentFieldTurnAnnouncementReplayed();
}
