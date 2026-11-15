using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Phase, Dialogauswahl und Sitzungsdaten des Assistenten vor und nach der Aktivierung zusammen,
/// damit Übergänge, Pausen und stille Eingaben denselben Zustand verwenden.
/// </summary>
public sealed record SpeechAssistantState(
    SpeechAssistantPhase Phase,
    SpeechAssistantDialogKind? ActiveDialog,
    string? CorrelationId,
    string? SelectedIsoCode,
    string? SelectedApplicationId,
    string? SelectedFormId,
    ImmutableDictionary<string, SpeechAssistantApplicationSessionState> ApplicationSessions,
    SpeechAssistantPauseSnapshot? PauseSnapshot = null,
    long SurfaceRevision = 0,
    SpeechAssistantFormSurfaceInteractionState? SilentInput = null,
    string? SessionId = null,
    string? SilentInputOperationId = null,
    FormSilentInputIntentKind? SilentInputPendingIntent = null,
    StudySession? StudySession = null)
{
    public static SpeechAssistantState Initial => new(
        SpeechAssistantPhase.Idle,
        null,
        null,
        null,
        null,
        null,
        ImmutableDictionary.Create<string, SpeechAssistantApplicationSessionState>(StringComparer.OrdinalIgnoreCase));

    public bool HasSelectedLanguage => !string.IsNullOrWhiteSpace(SelectedIsoCode);

    public bool HasSelectedApplication => !string.IsNullOrWhiteSpace(SelectedApplicationId);

    public bool HasSelectedForm => !string.IsNullOrWhiteSpace(SelectedFormId);
}


