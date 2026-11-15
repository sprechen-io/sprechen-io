namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die Entscheidung zur Bestätigung des aktuellen Feldwerts.
/// Die Fabrik <see cref="Undecided"/> nimmt einen Grund für eine gezielte
/// Rückfrage entgegen. Der öffentliche Konstruktor prüft die Kombination
/// der übergebenen Werte nicht.
/// </summary>
public readonly record struct ConfirmationDecision(
    ConfirmationDecisionKind Kind,
    string CommandId,
    string? CommandArgument,
    ConfirmationUndecidedReason? UndecidedReason = null)
{
    public static ConfirmationDecision Undecided(ConfirmationUndecidedReason reason)
        => new(ConfirmationDecisionKind.Undecided, string.Empty, null, reason);

    public static ConfirmationDecision Continue()
        => new(ConfirmationDecisionKind.Continuation, string.Empty, null);

    public static ConfirmationDecision Edit()
        => new(ConfirmationDecisionKind.CurrentValueEdit, string.Empty, null);

    public static ConfirmationDecision SwitchToSpelling()
        => new(ConfirmationDecisionKind.SpellingSwitch, string.Empty, null);

    public static ConfirmationDecision ExecuteCommand(string commandId, string? commandArgument)
        => new(ConfirmationDecisionKind.Execution, commandId, commandArgument);
}
