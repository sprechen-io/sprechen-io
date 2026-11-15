using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Startet nach dem Aktivierungswort entweder das Studienformular oder den Anwendungskatalog.
/// </summary>
internal static class SpeechAssistantArmedReducer
{
    internal static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> Reduce(
        SpeechAssistantState state,
        SpeechAssistantEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            SpeechAssistantLanguageSelectedEvent languageSelected => StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
                state with { Phase = SpeechAssistantPhase.Completed, ActiveDialog = null, SelectedIsoCode = languageSelected.NormalizedIsoCode },
                SpeechAssistantReducerTransitions.LanguageSelectionReturnToArmedEffects(languageSelected)),

            SpeechAssistantPressedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithResolvedLanguage(state),

            SpeechAssistantWakeWordListenerFailedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithResolvedLanguage(state),

            // Nach einer fehlgeschlagenen Aktivierungserkennung die Rückkehr zur Bereitschaft
            // anfordern. Eine volle Erkennungswarteschlange weist sofort ab. Dieses Ereignis belegt
            // deshalb kein zuvor abgewartetes Rücksetzintervall.
            SpeechAssistantWakeWordRecognitionFailedEvent => SpeechAssistantReducerTransitions.CompleteAndQueueReturnToArmedWithResolvedLanguage(state),

            SpeechAssistantWakeWordMissedEvent =>
                StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithoutEffects(state),

            SpeechAssistantWakeWordDetectedEvent wakeWordDetected when state.StudySession is not null =>
                StartStudyFormFromWakeWord(state, wakeWordDetected),

            SpeechAssistantWakeWordDetectedEvent wakeWordDetected when wakeWordDetected.HasCommandText =>
                StartFormsCatalogFromWakeWord(state, wakeWordDetected),

            SpeechAssistantWakeWordDetectedEvent wakeWordDetected =>
                StartFormsCatalogAnnouncementFromWakeWord(state, wakeWordDetected),

            _ => SpeechAssistantReducerTransitions.UnsupportedEvent(@event, SpeechAssistantPhase.Armed)
        };
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> StartStudyFormFromWakeWord(
        SpeechAssistantState state,
        SpeechAssistantWakeWordDetectedEvent wakeWordDetected)
    {
        var studySession = state.StudySession
            ?? throw new StudyBindFailedException("Study form start requires a bound study session.");
        var correlationId = ResolveCorrelationId(wakeWordDetected.CorrelationId);
        var nextState = state with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.VoiceCommand,
            CorrelationId = correlationId,
            SelectedApplicationId = studySession.ApplicationId,
            SelectedFormId = studySession.FormId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunVoiceCommandDialogEffect(
                state.SelectedIsoCode,
                studySession.ApplicationId,
                studySession.FormId,
                correlationId,
                SpeechAssistantApplicationSessionState.Empty));
    }

    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> StartFormsCatalogFromWakeWord(
        SpeechAssistantState state,
        SpeechAssistantWakeWordDetectedEvent wakeWordDetected)
    {
        var correlationId = ResolveCorrelationId(wakeWordDetected.CorrelationId);
        var nextState = state with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            CorrelationId = correlationId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunFormsCatalogDialogEffect(
                state.SelectedIsoCode,
                new SpeechTranscript(wakeWordDetected.CommandText, wakeWordDetected.ConfidencePercent),
                correlationId));
    }

    // Ohne angehängten Befehl den Katalogdialog ohne Anfangstranskript anfordern, damit dessen
    // Einstieg die Anwendungsauswahl ankündigt.
    private static StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext> StartFormsCatalogAnnouncementFromWakeWord(
        SpeechAssistantState state,
        SpeechAssistantWakeWordDetectedEvent wakeWordDetected)
    {
        var correlationId = ResolveCorrelationId(wakeWordDetected.CorrelationId);
        var nextState = state with
        {
            Phase = SpeechAssistantPhase.Engaged,
            ActiveDialog = SpeechAssistantDialogKind.FormsCatalog,
            CorrelationId = correlationId
        };

        return StateUpdate<SpeechAssistantState, SpeechAssistantEvent, SpeechAssistantContext>.WithEffects(
            nextState,
            new RunFormsCatalogDialogEffect(state.SelectedIsoCode, correlationId: correlationId));
    }

    private static string ResolveCorrelationId(string? correlationId)
        => string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId.Trim();
}
