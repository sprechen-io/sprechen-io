
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Dialogzustandswechsel, Rückmeldungsmerker und den Zähler betretener Bestätigungsschritte zusammen, damit die Interaktionswege denselben Laufzustand verwenden.
/// </summary>
internal sealed class FormVoiceCommandStateTracker : IFormVoiceCommandDialogStateTracker
{
    private readonly FormVoiceCommandTelemetryStateEmitter _telemetryStates;
    private readonly FormVoiceCommandFieldGroupPromptState _fieldGroupPromptState = new();
    private string? _currentMicroState;
    private int _reparableConfirmationTurnCount;
    private readonly HashSet<string> _taughtTeachingTopics = new(StringComparer.Ordinal);
    private string? _structuralTeachingReteachFieldId;
    private int? _fieldEntryAnswerableTotal;

    public FormVoiceCommandStateTracker(IEvaluationSessionTelemetry telemetry, EvaluationSession session)
    {
        _telemetryStates = new FormVoiceCommandTelemetryStateEmitter(telemetry, session);
    }

    internal string CurrentMacroState => _fieldGroupPromptState.CurrentMacroState;

    internal string? CurrentMicroState => _currentMicroState;

    internal string CurrentTelemetryState => _telemetryStates.CurrentTelemetryState;

    /// <summary>
    /// Zählt die begonnenen Bestätigungsschritte dieser Instanz. Die Ansage
    /// verwendet den Zähler für ausführliche oder verkürzte Hinweise.
    /// </summary>
    internal int ReparableConfirmationTurnCount => _reparableConfirmationTurnCount;

    /// <summary>
    /// Speichert AllFields.Total beim Einstieg in das aktuelle Feld.
    /// Ohne diesen Ausgangswert wird keine Änderung der Gesamtzahl angesagt.
    /// </summary>
    internal int? FieldEntryAnswerableTotal => _fieldEntryAnswerableTotal;

    // Das Thema beim ersten Aufruf vormerken und als noch nicht behandelt melden.
    // Weitere Aufrufe derselben Instanz erkennen den Merker. Die Wiedergabe folgt beim Aufrufer.
    public bool HasAlreadyTaught(string teachingTopicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teachingTopicId);
        return !_taughtTeachingTopics.Add(teachingTopicId);
    }

    internal void ArmStructuralTeachingReteach(string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        _structuralTeachingReteachFieldId = fieldId;
    }

    internal bool ConsumeStructuralTeachingReteach(string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        var normalizedFieldId = fieldId;
        if (!string.Equals(_structuralTeachingReteachFieldId, normalizedFieldId, StringComparison.Ordinal))
        {
            return false;
        }

        _structuralTeachingReteachFieldId = null;
        return true;
    }

    internal bool ShouldAnnounceFieldGroupIntro(string fieldGroupId)
        => _fieldGroupPromptState.ShouldAnnounceFieldGroupIntro(fieldGroupId);

    internal void RecordFieldEntryAnswerableTotal(int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(total);
        _fieldEntryAnswerableTotal = total;
    }

    internal void BeginFieldPrompt(string fieldGroupId)
    {
        _fieldEntryAnswerableTotal = null;
        _fieldGroupPromptState.BeginFieldPrompt(fieldGroupId);

        if (DialogMicroStatePromptGate.IsActivePromptState(_currentMicroState))
        {
            return;
        }

        _currentMicroState = DialogMicroStateIds.Prompting;
        _telemetryStates.Emit(DialogStates.Prompting);
    }

    internal void BeginListening()
    {
        var currentMicroState = GetRequiredCurrentMicroState();
        if (string.Equals(currentMicroState, DialogMicroStateIds.Listening, StringComparison.Ordinal)
            || string.Equals(currentMicroState, DialogMicroStateIds.Interpreting, StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(currentMicroState, DialogMicroStateIds.Prompting, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                currentMicroState,
                DialogMicroTransitionTriggers.PromptComplete,
                DialogMicroStateIds.Listening);
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        if (string.Equals(currentMicroState, DialogMicroStateIds.ErrorPrompt, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                currentMicroState,
                DialogMicroTransitionTriggers.ErrorPromptComplete,
                DialogMicroStateIds.Listening);
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        if (string.Equals(currentMicroState, DialogMicroStateIds.Repair, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                currentMicroState,
                DialogMicroTransitionTriggers.RepairComplete,
                DialogMicroStateIds.Listening);
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        throw new InvalidOperationException(
            $"Dialog micro state '{currentMicroState}' cannot enter the semantic listening flow.");
    }

    internal void RecordNoInputTimeout()
    {
        if (string.Equals(_currentMicroState, DialogMicroStateIds.Prompting, StringComparison.Ordinal))
        {
            return;
        }

        // Aktive Ansagezustände ohne passenden Übergang für fehlende Eingabe auf
        // Prompting zurücksetzen. Andere Zustände durchlaufen den modellierten
        // Übergang und können dabei als unzulässig zurückgewiesen werden.
        if (DialogMicroStatePromptGate.IsActivePromptState(_currentMicroState))
        {
            _currentMicroState = DialogMicroStateIds.Prompting;
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.NoInputTimeout,
            DialogMicroStateIds.Prompting);
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);
    }

    internal void BeginInterpretation()
    {
        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.SpeechReceived,
            DialogMicroStateIds.Interpreting);
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);
    }

    internal void BeginValidation()
    {
        if (string.Equals(_currentMicroState, DialogMicroStateIds.Listening, StringComparison.Ordinal))
        {
            // Wenn die Wertübernahme noch im Zuhören liegt, zuerst den modellierten
            // Übergang für empfangene Sprache setzen. Die Prüfung erwartet den Zustand
            // nach abgeschlossener Interpretation.
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                GetRequiredCurrentMicroState(),
                DialogMicroTransitionTriggers.SpeechReceived,
                DialogMicroStateIds.Interpreting);
        }

        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.InterpretationComplete,
            DialogMicroStateIds.Validating);
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);
    }

    internal void BeginFieldConfirmation()
    {
        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.ValidationPassed,
            DialogMicroStateIds.Confirmation);
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);

        // Jeden Beginn einer Bestätigung zählen, auch nach einer Korrektur.
        // Der Zähler steuert die Hinweisform und ist kein Wiedergabenachweis.
        _reparableConfirmationTurnCount++;
    }

    internal void BeginErrorPrompt()
    {
        if (string.Equals(_currentMicroState, DialogMicroStateIds.Interpreting, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                GetRequiredCurrentMicroState(),
                DialogMicroTransitionTriggers.InterpretationComplete,
                DialogMicroStateIds.Validating);
        }

        if (string.Equals(_currentMicroState, DialogMicroStateIds.Validating, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                GetRequiredCurrentMicroState(),
                DialogMicroTransitionTriggers.ValidationFailed,
                DialogMicroStateIds.ErrorPrompt);
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        if (string.Equals(_currentMicroState, DialogMicroStateIds.Listening, StringComparison.Ordinal))
        {
            _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
                GetRequiredCurrentMicroState(),
                DialogMicroTransitionTriggers.MaxRetriesExceeded,
                DialogMicroStateIds.ErrorPrompt);
            _telemetryStates.EmitProjectedMicroState(_currentMicroState);
            return;
        }

        throw new InvalidOperationException(
            $"Dialog micro state '{_currentMicroState ?? "<unset>"}' cannot enter the semantic error-prompt flow.");
    }

    internal void CompleteConfirmedField()
    {
        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.UserConfirmed,
            expectedNextState: null);
    }

    internal void BeginFieldRepair()
    {
        _currentMicroState = FormDialogStateMachine.ApplyMicroTransition(
            GetRequiredCurrentMicroState(),
            DialogMicroTransitionTriggers.UserRejected,
            DialogMicroStateIds.Repair);
        // Den projizierten Telemetriezustand melden, damit der Wechsel von Bestätigung
        // zu Reparatur im Zustandsbericht enthalten ist.
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);
    }

    internal void BeginFieldPromptRestart()
    {
        var currentMicroState = GetRequiredCurrentMicroState();
        if (!string.Equals(currentMicroState, DialogMicroStateIds.Interpreting, StringComparison.Ordinal)
            && !string.Equals(currentMicroState, DialogMicroStateIds.Confirmation, StringComparison.Ordinal)
            && !string.Equals(currentMicroState, DialogMicroStateIds.ErrorPrompt, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Dialog micro state '{currentMicroState}' cannot enter the field-prompt restart transition.");
        }

        _currentMicroState = DialogMicroStateIds.Prompting;
        _telemetryStates.EmitProjectedMicroState(_currentMicroState);
    }

    internal void BeginCancelConfirmation()
    {
        _telemetryStates.Emit(DialogStates.CancelConfirmation);
    }

    internal void RecordAutoAdvanceToNextGroup(string nextFieldGroupId)
    {
        _fieldGroupPromptState.RecordAutoAdvanceToNextGroup(nextFieldGroupId);
    }

    internal void BeginClosingReview()
    {
        _fieldGroupPromptState.BeginClosingReview();
        if (!DialogMicroStatePromptGate.IsActivePromptState(_currentMicroState))
        {
            _currentMicroState = DialogMicroStateIds.Prompting;
            _telemetryStates.Emit(DialogStates.Prompting);
        }
    }

    internal void RecordClosingReviewChangeRequested()
        => _fieldGroupPromptState.RecordClosingReviewChangeRequested();

    internal void RecordSubmitted()
    {
        _currentMicroState = null;
        _fieldGroupPromptState.RecordSubmitted();
    }

    internal void RecordCancelled()
    {
        if (!_fieldGroupPromptState.TryRecordCancelled())
        {
            return;
        }

        _currentMicroState = null;
    }

    private string GetRequiredCurrentMicroState()
    {
        return _currentMicroState
            ?? throw new InvalidOperationException("Dialog micro state is not set for the requested transition.");
    }

    void IFormVoiceCommandDialogStateTracker.BeginErrorPrompt()
    {
        BeginErrorPrompt();
    }

    void IFormVoiceCommandDialogStateTracker.BeginCancelConfirmation()
    {
        BeginCancelConfirmation();
    }

    void IFormVoiceCommandDialogStateTracker.RecordSubmitted()
    {
        RecordSubmitted();
    }

    void IFormVoiceCommandDialogStateTracker.RecordCancelled()
    {
        RecordCancelled();
    }
}
