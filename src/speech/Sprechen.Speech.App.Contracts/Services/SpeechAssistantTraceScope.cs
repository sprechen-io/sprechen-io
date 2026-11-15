namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kennzeichnet den fachlichen Ursprung einer Diagnosemeldung zur Zuordnung im Assistentenablauf.
/// </summary>
public enum SpeechAssistantTraceScope
{
    WakeWordEffect = 0,
    FormsCatalogEffect = 2,
    FormSelectionEffect = 3,
    VoiceCommandEffect = 4,
    LanguageChangeEffect = 5,
    PolitenessChangeEffect = 6,
    FormsCatalogDialog = 7,
    SelectionDialogRuntime = 8,
    Reducer = 9,
    PostWakeWordRecovery = 10
}
