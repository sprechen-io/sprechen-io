using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt Sprache, Anwendung und Formular mit den benötigten Abhängigkeiten
/// für eine Formularsitzung mit Spracheingabe.
/// </summary>
public interface IFormVoiceCommandContextLoader
{
    Task<FormVoiceCommandContextLoadResult> LoadAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken);
}

