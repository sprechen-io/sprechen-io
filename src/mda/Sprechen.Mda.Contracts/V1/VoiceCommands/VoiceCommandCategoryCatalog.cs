namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Ordnet Sprachkommandokennungen den Kategorien der vorgelesenen Kommandoliste zu.
/// Die Kennungen sind Zeichenketten aus <c>VoiceCommandId</c> im JSON-Schema,
/// da diese Assembly nicht von der darüberliegenden Sprachassistentenschicht
/// mit ihren <c>GlobalCommandIds</c>-Konstanten abhängen soll.
/// </summary>
public static class VoiceCommandCategoryCatalog
{
    private static readonly IReadOnlyDictionary<string, VoiceCommandCategory> CategoryByCommandId =
        new Dictionary<string, VoiceCommandCategory>(StringComparer.Ordinal)
        {
            // Navigation: Fokus im Formular bewegen.
            ["next"] = VoiceCommandCategory.Navigation,
            ["back"] = VoiceCommandCategory.Navigation,
            ["jumpTo"] = VoiceCommandCategory.Navigation,
            ["nextError"] = VoiceCommandCategory.Navigation,
            ["nextOpenRequiredField"] = VoiceCommandCategory.Navigation,

            // Monitoring: aktuellen Stand vorlesen.
            ["repeat"] = VoiceCommandCategory.Monitoring,
            ["status"] = VoiceCommandCategory.Monitoring,
            ["summary"] = VoiceCommandCategory.Monitoring,
            ["formStatus"] = VoiceCommandCategory.Monitoring,

            // Discovery: verfügbare Möglichkeiten aufzählen.
            ["help"] = VoiceCommandCategory.Discovery,
            ["formList"] = VoiceCommandCategory.Discovery,
            ["fieldGroupList"] = VoiceCommandCategory.Discovery,
            ["fieldList"] = VoiceCommandCategory.Discovery,
            ["commandList"] = VoiceCommandCategory.Discovery,

            // InputMode: Eingabemodus und Felderfassung steuern. Dazu gehören auch
            // Ziffern- und Buchstabierkommandos, die nicht in commandList angeboten werden.
            ["inputModeSpelling"] = VoiceCommandCategory.InputMode,
            ["inputModeVoice"] = VoiceCommandCategory.InputMode,
            ["inputModeDigits"] = VoiceCommandCategory.InputMode,
            ["inputModeKeyboard"] = VoiceCommandCategory.InputMode,
            ["inputModeLiteral"] = VoiceCommandCategory.InputMode,
            ["inputModeLiteralOff"] = VoiceCommandCategory.InputMode,
            ["digitsDeleteLastDigit"] = VoiceCommandCategory.InputMode,
            ["digitsDeleteLastBlock"] = VoiceCommandCategory.InputMode,
            ["digitsRepeatLastBlock"] = VoiceCommandCategory.InputMode,
            ["digitsRedictateLastBlock"] = VoiceCommandCategory.InputMode,
            ["spellingRemoveLastCharacter"] = VoiceCommandCategory.InputMode,
            ["spellingRemoveLastBlock"] = VoiceCommandCategory.InputMode,
            ["spellingClearBuffer"] = VoiceCommandCategory.InputMode,
            ["spellingReadback"] = VoiceCommandCategory.InputMode,

            // Control: Prozess, Sitzung, Formular und Anwendung steuern sowie Werte korrigieren.
            ["submit"] = VoiceCommandCategory.Control,
            ["cancel"] = VoiceCommandCategory.Control,
            ["editValue"] = VoiceCommandCategory.Control,
            ["acceptValue"] = VoiceCommandCategory.Control,
            ["switchForm"] = VoiceCommandCategory.Control,
            ["backToFormSelection"] = VoiceCommandCategory.Control,
            ["backToAppSelection"] = VoiceCommandCategory.Control,
            ["changeLanguage"] = VoiceCommandCategory.Control,
            ["changePoliteness"] = VoiceCommandCategory.Control,
            ["openApplications"] = VoiceCommandCategory.Control,
            ["allowBargeInOn"] = VoiceCommandCategory.Control,
            ["allowBargeInOff"] = VoiceCommandCategory.Control,
        };

    /// <summary>
    /// Vorlesereihenfolge der Kategorien. Kategorien ohne angebotene Kommandos
    /// werden bei der Ausgabe ausgelassen.
    /// </summary>
    public static IReadOnlyList<VoiceCommandCategory> RenderOrder { get; } =
    [
        VoiceCommandCategory.Navigation,
        VoiceCommandCategory.Monitoring,
        VoiceCommandCategory.Discovery,
        VoiceCommandCategory.InputMode,
        VoiceCommandCategory.Control,
    ];

    /// <summary>
    /// Vollständige Zuordnung der katalogisierten Kommandokennungen zu Kategorien.
    /// </summary>
    public static IReadOnlyDictionary<string, VoiceCommandCategory> All => CategoryByCommandId;

    /// <summary>
    /// Liefert die Kategorie einer katalogisierten Kommandokennung.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Die Kommando-Id ist nicht kategorisiert.</exception>
    public static VoiceCommandCategory CategoryOf(string commandId)
    {
        ArgumentNullException.ThrowIfNull(commandId);
        return CategoryByCommandId.TryGetValue(commandId, out var category)
            ? category
            : throw new KeyNotFoundException($"Voice command '{commandId}' has no category.");
    }
}
