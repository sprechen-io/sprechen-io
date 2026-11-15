using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Verbindet Formularbeobachtungen mit Ereignisdaten und gespeichertem Sitzungszustand, um Fokuswechsel, behobene Fehler und Statusänderungen zu melden.
/// </summary>
internal sealed class EvaluationFormTelemetry(
    EvaluationTelemetrySessionStateStore sessionStateStore,
    EvaluationTelemetryEnvelopeEmitter envelopeEmitter,
    EvaluationFieldTelemetryPayloads fieldPayloads,
    EvaluationStatusTelemetryPayloads statusPayloads,
    EvaluationConversationalPacingTelemetry pacingTelemetry,
    EvaluationTelemetryDetailedStatusChangeDetector detailedStatusChangeDetector) : IEvaluationFormTelemetry
{
    private readonly EvaluationConversationalPacingTelemetry _pacingTelemetry =
        pacingTelemetry ?? throw new ArgumentNullException(nameof(pacingTelemetry));
    private readonly EvaluationTelemetryDetailedStatusChangeDetector _detailedStatusChangeDetector =
        detailedStatusChangeDetector ?? throw new ArgumentNullException(nameof(detailedStatusChangeDetector));
    private readonly EvaluationTelemetryEnvelopeEmitter _envelopeEmitter =
        envelopeEmitter ?? throw new ArgumentNullException(nameof(envelopeEmitter));
    private readonly EvaluationFieldTelemetryPayloads _fieldPayloads =
        fieldPayloads ?? throw new ArgumentNullException(nameof(fieldPayloads));
    private readonly EvaluationTelemetrySessionStateStore _sessionStateStore =
        sessionStateStore ?? throw new ArgumentNullException(nameof(sessionStateStore));
    private readonly EvaluationStatusTelemetryPayloads _statusPayloads =
        statusPayloads ?? throw new ArgumentNullException(nameof(statusPayloads));

    public void TrackFieldFocus(EvaluationSession session, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_sessionStateStore.TryUpdateFocusedField(session.SessionId, fieldId))
        {
            return;
        }

        Emit(
            session,
            TelemetryEventNames.FocusChange,
            _fieldPayloads.CreateFieldFocus(fieldId));
    }

    public void TrackFieldAttempt(
        EvaluationSession session,
        string fieldId,
        string inputMode,
        bool success,
        int? sttConfidencePercent = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!success)
        {
            _pacingTelemetry.TrackFieldAttempt(
                session,
                fieldId,
                inputMode,
                success,
                sttConfidencePercent);
            return;
        }

        Emit(
            session,
            TelemetryEventNames.ValueChange,
            _fieldPayloads.CreateFieldAttempt(fieldId, sttConfidencePercent));
        _pacingTelemetry.TrackFieldAttempt(
            session,
            fieldId,
            inputMode,
            success,
            sttConfidencePercent);
        EmitValidationErrorsClearedForField(session, fieldId);
    }

    public void TrackNoSpeechDetected(EvaluationSession session, string fieldId)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.NoSpeechDetected,
            _fieldPayloads.CreateNoSpeechDetected(fieldId));
        _pacingTelemetry.TrackNoSpeechDetected(session, fieldId);
    }

    public void TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string trigger)
    {
        TrackConstraintViolation(session, fieldId, errorCode, severity, ConstraintScopes.Field, fieldGroupId: null, trigger);
    }

    public void TrackConstraintViolation(
        EvaluationSession session,
        string fieldId,
        string errorCode,
        string severity,
        string scope,
        string? fieldGroupId,
        string trigger)
    {
        ArgumentNullException.ThrowIfNull(session);

        var classifiedError = EvaluationTelemetryErrorClassifier.ClassifyConstraintViolation(
            fieldId,
            errorCode,
            severity,
            scope,
            fieldGroupId,
            trigger);

        switch (classifiedError.Route)
        {
            case TelemetryStudyErrorRoute.Validation:
                // Nur das Validierungsereignis trägt den Auslöser. Gemerkt wird der interne
                // Code, weil er den offenen Fehler benennt. Geschrieben wird die Diagnose.
                Emit(
                    session,
                    TelemetryEventNames.ValidationErrorOpened,
                    _fieldPayloads.CreateValidationError(
                        TelemetryStudyDiagnosisCodes.Validation,
                        classifiedError.Severity,
                        classifiedError.Scope,
                        classifiedError.FieldId,
                        classifiedError.FieldGroupId,
                        classifiedError.Trigger));
                RememberValidationError(
                    session,
                    errorCode: classifiedError.ErrorCode,
                    severity: classifiedError.Severity,
                    scope: classifiedError.Scope,
                    fieldId: classifiedError.FieldId,
                    fieldGroupId: classifiedError.FieldGroupId);
                return;
            case TelemetryStudyErrorRoute.Recognition:
                Emit(
                    session,
                    TelemetryEventNames.SttError,
                    _fieldPayloads.CreateError(
                        TelemetryStudyDiagnosisCodes.SpeechRecognition,
                        classifiedError.Severity,
                        classifiedError.Scope,
                        classifiedError.FieldId,
                        classifiedError.FieldGroupId));
                return;
            case TelemetryStudyErrorRoute.Silence:
                // Ausbleibende Sprache ist kein Erkennerausfall. Dieser Zweig meldet sie als eigenes Ereignis statt als Studienfehler.
                TrackNoSpeechDetected(session, classifiedError.FieldId);
                return;
            case TelemetryStudyErrorRoute.None:
                // Ein allein vom Transkript abhängiges Urteil belegt weder eine Regelverletzung noch einen Erkennerausfall.
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(classifiedError),
                    classifiedError.Route,
                    "A new study error route must state which event it writes before a reported code "
                    + "can take it; falling through here would drop the line without saying so.");
        }
    }

    public void TrackNavigationError(EvaluationSession session, string errorCode, string scope)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        var loggingScope = EvaluationTelemetryErrorClassifier.MapScope(scope);

        // Das Ereignis erhält den Diagnosecode der Studie statt des internen Dialogcodes.
        Emit(
            session,
            TelemetryEventNames.NavigationError,
            _fieldPayloads.CreateError(
                TelemetryStudyDiagnosisCodes.Navigation,
                TelemetrySeverityLevels.Error,
                loggingScope,
                fieldId: null,
                fieldGroupId: null));
    }

    public void TrackNavigationAidNoTarget(EvaluationSession session, string commandId)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.SystemFeedback,
            _fieldPayloads.CreateSystemFeedback(TelemetrySystemFeedbackTypes.NavigationNoTarget, commandId));
    }

    public void TrackStatusSnapshot(EvaluationSession session, EvaluationFormStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(snapshot);

        var navigationState = new EvaluationStatusNavigationSnapshot(
            snapshot.Navigation.CurrentFieldId,
            snapshot.Navigation.CurrentFieldGroupId,
            snapshot.Navigation.Index,
            snapshot.Navigation.Total);

        if (_sessionStateStore.TryUpdateNavigation(session.SessionId, navigationState))
        {
            Emit(
                session,
                TelemetryEventNames.NavigationChanged,
                _statusPayloads.CreateNavigation(snapshot));
        }

        Emit(
            session,
            TelemetryEventNames.StatusSnapshot,
            _statusPayloads.CreateStatusSnapshot(snapshot));
    }

    public void TrackFieldAndGroupStatus(EvaluationSession session, EvaluationDetailedFormStatus snapshot)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(snapshot);

        var previousSnapshot = _sessionStateStore.GetLastDetailedStatus(session.SessionId);
        if (previousSnapshot is null)
        {
            var completionTransitions = snapshot.Fields
                .OrderBy(static field => field.FieldId, StringComparer.Ordinal)
                .SelectMany(static field => TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged(
                    field.FieldId,
                    field.Completed))
                .Concat(snapshot.FieldGroups
                    .OrderBy(static group => group.FieldGroupId, StringComparer.Ordinal)
                    .SelectMany(static group => TelemetryCanonicalTransitionProjection.ForFieldGroupCompletionChanged(
                        group.FieldGroupId,
                        group.Completed)))
                .ToArray();
            this._envelopeEmitter.PrimeFieldAndGroupCompletionAdoption(session, completionTransitions);
            _sessionStateStore.SetLastDetailedStatus(session.SessionId, snapshot);
            return;
        }

        var changes = _detailedStatusChangeDetector.DetectChanges(previousSnapshot, snapshot);
        this._envelopeEmitter.EmitBatch(session, changes);
        _sessionStateStore.SetLastDetailedStatus(session.SessionId, snapshot);
    }

    public void TrackRepairAttempt(EvaluationSession session, string fieldId, string repairMode)
    {
        ArgumentNullException.ThrowIfNull(session);

        Emit(
            session,
            TelemetryEventNames.SystemFeedback,
            _fieldPayloads.CreateSystemFeedback(TelemetrySystemFeedbackTypes.Help));
        _pacingTelemetry.TrackRepairAttempt(session, fieldId, repairMode);
    }

    public void TrackInputModeChanged(
        EvaluationSession session,
        string fieldId,
        string fromMode,
        string toMode,
        string trigger)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.Equals(fromMode, toMode, StringComparison.Ordinal))
        {
            return;
        }

        Emit(
            session,
            TelemetryEventNames.InputModeChanged,
            _fieldPayloads.CreateInputModeChanged(
                fieldId,
                fromMode,
                toMode,
                trigger));
    }

    private void Emit(EvaluationSession session, string eventName, object? data)
    {
        _envelopeEmitter.Emit(session, eventName, data);
    }

    private void EmitValidationErrorsClearedForField(EvaluationSession session, string fieldId)
    {
        foreach (var removed in _sessionStateStore.RemoveValidationErrorsForField(session.SessionId, fieldId))
        {
            Emit(
                session,
                TelemetryEventNames.ValidationErrorCleared,
                _fieldPayloads.CreateError(
                    TelemetryStudyDiagnosisCodes.Validation,
                    removed.Severity,
                    removed.Scope,
                    removed.FieldId,
                    removed.FieldGroupId));
        }
    }

    private void RememberValidationError(
        EvaluationSession session,
        string errorCode,
        string severity,
        string scope,
        string fieldId,
        string? fieldGroupId)
    {
        _sessionStateStore.RememberValidationError(
            session.SessionId,
            new EvaluationTelemetryErrorState(errorCode, severity, scope, fieldId, fieldGroupId));
    }
}
