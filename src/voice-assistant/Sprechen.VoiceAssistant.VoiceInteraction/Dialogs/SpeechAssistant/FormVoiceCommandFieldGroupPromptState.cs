namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet Gruppenansage und Makrozustand des Formularablaufs, damit Gruppeneinstieg, Abschlussprüfung und Rückkehr zur Bearbeitung geordnete Übergänge verwenden.
/// </summary>
internal sealed class FormVoiceCommandFieldGroupPromptState
{
    private string _currentMacroState = DialogMacroStateIds.FieldGroupIntro;
    private string? _currentFieldGroupId;
    private bool _awaitingFieldGroupIntroCompletion = true;

    internal string CurrentMacroState => _currentMacroState;

    internal bool ShouldAnnounceFieldGroupIntro(string fieldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupId);

        if (_awaitingFieldGroupIntroCompletion)
        {
            return true;
        }

        return IsNewFieldGroupInFieldLoop(fieldGroupId);
    }

    internal void BeginFieldPrompt(string fieldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupId);
        var normalizedFieldGroupId = fieldGroupId;

        if (IsNewFieldGroupInFieldLoop(normalizedFieldGroupId))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.JumpToFieldGroup,
                DialogMacroStateIds.FieldGroupIntro);
            _awaitingFieldGroupIntroCompletion = true;
        }

        if (_awaitingFieldGroupIntroCompletion)
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.IntroComplete,
                DialogMacroStateIds.FieldLoop);
            _awaitingFieldGroupIntroCompletion = false;
        }

        _currentFieldGroupId = normalizedFieldGroupId;
    }

    internal void RecordAutoAdvanceToNextGroup(string nextFieldGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nextFieldGroupId);

        _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
            _currentMacroState,
            DialogMacroTransitionTriggers.AllFieldsProcessed,
            DialogMacroStateIds.FieldGroupSummary);
        _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
            _currentMacroState,
            DialogMacroTransitionTriggers.SummaryAccepted,
            DialogMacroStateIds.FieldGroupIntro);
        _awaitingFieldGroupIntroCompletion = true;
        _currentFieldGroupId = nextFieldGroupId;
    }

    // Aus den behandelten Einstiegszuständen bis zum modellierten Prüfzustand
    // fortschalten und dort für den Abschlussdialog stehen bleiben.
    internal void BeginClosingReview()
    {
        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldGroupIntro, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.IntroComplete,
                DialogMacroStateIds.FieldLoop);
            _awaitingFieldGroupIntroCompletion = false;
        }

        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldLoop, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.AllFieldsProcessed,
                DialogMacroStateIds.FieldGroupSummary);
        }

        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldGroupSummary, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.SummaryAccepted,
                DialogMacroStateIds.ProcessSummary);
        }
    }

    // Den Prüfzustand über den Ablehnungsübergang verlassen und den
    // Gruppeneinstieg für den weiteren Feldablauf erneut freigeben.
    internal void RecordClosingReviewChangeRequested()
    {
        if (!string.Equals(_currentMacroState, DialogMacroStateIds.ProcessSummary, StringComparison.Ordinal))
        {
            return;
        }

        _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
            _currentMacroState,
            DialogMacroTransitionTriggers.ProcessRejected,
            DialogMacroStateIds.FieldGroupIntro);
        _awaitingFieldGroupIntroCompletion = true;
    }

    internal void RecordSubmitted()
    {
        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldGroupIntro, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.IntroComplete,
                DialogMacroStateIds.FieldLoop);
            _awaitingFieldGroupIntroCompletion = false;
        }

        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldLoop, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.AllFieldsProcessed,
                DialogMacroStateIds.FieldGroupSummary);
        }

        if (string.Equals(_currentMacroState, DialogMacroStateIds.FieldGroupSummary, StringComparison.Ordinal))
        {
            _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
                _currentMacroState,
                DialogMacroTransitionTriggers.SummaryAccepted,
                DialogMacroStateIds.ProcessSummary);
        }

        _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
            _currentMacroState,
            DialogMacroTransitionTriggers.ProcessConfirmed,
            DialogMacroStateIds.Submitted);
    }

    internal bool TryRecordCancelled()
    {
        if (string.Equals(_currentMacroState, DialogMacroStateIds.Cancelled, StringComparison.Ordinal))
        {
            return false;
        }

        _currentMacroState = FormDialogStateMachine.ApplyMacroTransition(
            _currentMacroState,
            DialogMacroTransitionTriggers.CancelConfirmed,
            DialogMacroStateIds.Cancelled);
        return true;
    }

    private bool IsNewFieldGroupInFieldLoop(string normalizedFieldGroupId)
        => _currentFieldGroupId is not null
            && !string.Equals(_currentFieldGroupId, normalizedFieldGroupId, StringComparison.Ordinal)
            && string.Equals(_currentMacroState, DialogMacroStateIds.FieldLoop, StringComparison.Ordinal)
            && !_awaitingFieldGroupIntroCompletion;
}
