namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Feld, Sitzung und Laufzeitdienste für einen Schritt der
/// Feldbearbeitung und prüft die erforderlichen Feld-, Variablen- und Gruppenkennungen.
/// </summary>
internal sealed record FormVoiceCommandFieldTurnContext
{
    public FormVoiceCommandFieldTurnContext(
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(autoHelp);
        ArgumentNullException.ThrowIfNull(escalation);
        ArgumentNullException.ThrowIfNull(privacyReadbackPolicy);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.FieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.VariableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.FieldGroupId);

        Context = context;
        Interpreter = interpreter;
        Field = field;
        Session = session;
        StateTracker = stateTracker;
        AutoHelp = autoHelp;
        Escalation = escalation;
        PrivacyReadbackPolicy = privacyReadbackPolicy;
    }

    public FormVoiceCommandContext Context { get; }

    public IFormDialogRuntime Interpreter { get; }

    public IFormDialogPrivacyReadbackPolicy PrivacyReadbackPolicy { get; }

    public FormFieldSnapshot Field { get; }

    /// <summary>
    /// Liefert den optional übergebenen Orientierungstext für die Aufnahmeansage.
    /// Die Auswahl eines Ersatztexts liegt beim verwendenden Ablauf.
    /// </summary>
    public string? FieldEntryPrompt { get; init; }

    public EvaluationSession Session { get; }

    public FormVoiceCommandStateTracker StateTracker { get; }

    public FormVoiceCommandAutoHelpTracker AutoHelp { get; }

    public FormVoiceCommandUnrecognizedInputEscalationPolicy Escalation { get; }
}
