using Sprechen.Common.Cqrs;
namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Stellt Fokusbezüge und Formularzähler für die Statusabfrage lesend bereit.
/// </summary>
public interface IReadableFormStatus
{
    string? CurrentFieldId { get; }
    string? CurrentFieldGroupId { get; }
    int CurrentFieldIndex { get; }
    int CurrentFieldGroupIndex { get; }
    int TotalFields { get; }
    int TotalFieldGroups { get; }
    int OpenRequiredCount { get; }
    int ValidationErrorCount { get; }
}

