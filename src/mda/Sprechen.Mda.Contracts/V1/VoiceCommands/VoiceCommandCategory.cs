namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Kategorie eines Sprachkommandos zur Gliederung der vorgelesenen Kommandoliste.
/// Die Zuordnung verändert das Kommandoverhalten nicht. Die Vorlesereihenfolge
/// legt <see cref="VoiceCommandCategoryCatalog.RenderOrder"/> fest.
/// </summary>
public enum VoiceCommandCategory
{
    /// <summary>
    /// Fokus im Formular bewegen: next, back, jumpTo, nextError und nextOpenRequiredField.
    /// </summary>
    Navigation,

    /// <summary>
    /// Aktuellen Stand vorlesen: repeat, status, summary und formStatus.
    /// </summary>
    Monitoring,

    /// <summary>
    /// Verfügbare Möglichkeiten aufzählen: help, formList, fieldGroupList, fieldList und commandList.
    /// </summary>
    Discovery,

    /// <summary>
    /// Eingabemodus und Erfassung des aktuellen Feldes steuern.
    /// </summary>
    InputMode,

    /// <summary>
    /// Prozess, Sitzung, Formular und Anwendung steuern sowie Werte korrigieren.
    /// </summary>
    Control,
}
