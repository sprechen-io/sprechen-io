namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Eigenschaftsnamen der Telemetrienutzlast aus telemetry-events.schema.json
/// und telemetry-fragments.schema.json.
/// </summary>
public static class TelemetryPayloadProperties
{
    #region shared identifiers

    public const string FieldId = "field_id";
    public const string FieldGroupId = "field_group_id";
    public const string CommandId = "command_id";

    #endregion

    #region shared state flags

    public const string Relevant = "relevant";
    public const string Required = "required";
    public const string Completed = "completed";
    public const string Total = "total";

    #endregion

    #region StatusNavigation (telemetry-fragments.schema.json#StatusNavigation)

    public const string CurrentFieldId = "current_field_id";
    public const string CurrentFieldGroupId = "current_field_group_id";
    public const string NavigationIndex = "navigation_index";
    public const string NavigationTotal = "navigation_total";

    #endregion

    #region StatusSnapshotData (telemetry-events.schema.json#StatusSnapshotData)

    public const string Navigation = "navigation";
    public const string Progress = "progress";
    public const string ZeroDenominatorMode = "zero_denominator_mode";

    #endregion

    #region StatusSnapshotProgress (telemetry-fragments.schema.json#StatusSnapshotProgress)

    public const string AllFields = "allFields";
    public const string RequiredFieldsOnly = "requiredFieldsOnly";
    public const string FieldGroupBased = "fieldGroupBased";

    #endregion


    #region TaskEndData

    public const string CanonicalTransitions = "canonical_transitions";

    public const string CanonicalClass = "canonical_class";

    public const string TransitionScope = "scope";

    public const string ObjectId = "object_id";

    public const string ResultingState = "resulting_state";
    public const string EndState = "end_state";
    public const string MismatchCode = "mismatch_code";
    public const string CorrectnessBasis = "correctness_basis";

    #endregion

    #region ErrorData (telemetry-fragments.schema.json#ErrorData)

    public const string ErrorCode = "error_code";
    public const string Severity = "severity";
    public const string Scope = "scope";
    public const string RelatedFieldIds = "related_field_ids";

    #endregion

    #region error code arrays

    public const string BlockingErrorCodes = "blocking_error_codes";
    public const string OpenGroupErrorCodes = "open_group_error_codes";

    #endregion

    #region ValueChangeData

    public const string SttConfidence = "stt_confidence";

    #endregion

    #region ConversationalPacingSignalData

    public const string SignalType = "signal_type";
    public const string InputMode = "input_mode";
    public const string Success = "success";
    public const string AttemptNumber = "attempt_number";
    public const string ResponseLatencyMs = "response_latency_ms";
    public const string RepairMode = "repair_mode";

    #endregion

    #region IntentRecognizedData

    public const string Intent = "intent";

    #endregion

    #region CommandRecognitionFailedData

    /// <summary>
    /// Begründungscode für ein abgelehntes oder nicht erkanntes Sprachkommando.
    /// </summary>
    public const string FailureReason = "failure_reason";

    #endregion

    #region SystemFeedbackData

    public const string Type = "type";

    #endregion

    #region ManualStepData

    public const string Note = "note";

    #endregion

    #region AmbiguityResolvedData

    public const string AmbiguityType = "ambiguity_type";
    public const string ResolutionStrategy = "resolution_strategy";
    public const string SignalClass = "signal_class";
    public const string Result = "result";

    #endregion

    #region InputModeChangedData

    public const string FromMode = "from_mode";
    public const string ToMode = "to_mode";
    public const string Trigger = "trigger";

    #endregion

    #region DestructiveCommandResolvedData

    public const string Confirmed = "confirmed";
    public const string RefractoryBlocked = "refractory_blocked";

    #endregion

    #region BargeInData

    public const string InterruptedPromptId = "interrupted_prompt_id";

    #endregion

    #region wake-word telemetry data

    public const string Phase = "phase";
    public const string EventCode = "event_code";
    public const string LanguageCode = "language_code";
    public const string DetectorId = "detector_id";
    public const string ActivationFormCount = "activation_form_count";
    public const string RecognitionOnlyActivationFormCount = "recognition_only_activation_form_count";
    public const string ExceptionType = "exception_type";
    public const string FailureCode = "failure_code";
    public const string Status = "status";
    public const string ConfidencePercent = "confidence_percent";
    public const string CommandTextPresent = "command_text_present";
    public const string TranscriptTextLength = "transcript_text_length";
    public const string StitchedTranscriptLength = "stitched_transcript_length";
    public const string CommandTextLength = "command_text_length";

    #endregion

    #region FieldRelevanceChangedData / FieldGroupRelevanceChangedData

    /// <summary>
    /// Nutzlasteigenschaft für modellierte Begründungs- und Ersatzcodes.
    /// </summary>
    public const string ReasonCode = "reason_code";

    #endregion

    #region LanguageDetectedData (telemetry-events.schema.json#LanguageDetectedData)

    /// <summary>
    /// Erkannte ISO-639-1-Sprachcodes für den Gesprächsschritt.
    /// </summary>
    public const string DetectedLanguageCodes = "detected_language_codes";

    /// <summary>
    /// Sprachcodes der beim Gesprächsschritt aktiven Erkennungshinweise.
    /// </summary>
    public const string HintedLanguageCodes = "hinted_language_codes";

    /// <summary>
    /// Optionaler Index des Gesprächsschritts, auf den sich die Sprachsignale beziehen.
    /// </summary>
    public const string TurnIndex = "turn_index";

    #endregion
}
