using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft angeforderte Makro- und Mikrozustandsübergänge gegen die erlaubten Kombinationen,
/// ohne den Formularzustand selbst zu verändern.
/// </summary>
public static class FormDialogStateMachine
{
    private static readonly ImmutableArray<FormDialogMacroTransition> MacroTransitions =
    [
        new(DialogMacroStateIds.FieldGroupIntro, DialogMacroTransitionTriggers.IntroComplete, DialogMacroStateIds.FieldLoop),
        new(DialogMacroStateIds.FieldGroupIntro, DialogMacroTransitionTriggers.CancelConfirmed, DialogMacroStateIds.Cancelled),
        new(DialogMacroStateIds.FieldLoop, DialogMacroTransitionTriggers.AllFieldsProcessed, DialogMacroStateIds.FieldGroupSummary),
        new(DialogMacroStateIds.FieldLoop, DialogMacroTransitionTriggers.NavigateNext, DialogMacroStateIds.FieldGroupIntro),
        new(DialogMacroStateIds.FieldLoop, DialogMacroTransitionTriggers.NavigateBack, DialogMacroStateIds.FieldGroupIntro),
        new(DialogMacroStateIds.FieldLoop, DialogMacroTransitionTriggers.JumpToFieldGroup, DialogMacroStateIds.FieldGroupIntro),
        new(DialogMacroStateIds.FieldLoop, DialogMacroTransitionTriggers.CancelConfirmed, DialogMacroStateIds.Cancelled),
        new(DialogMacroStateIds.FieldGroupSummary, DialogMacroTransitionTriggers.SummaryAccepted, DialogMacroStateIds.FieldGroupIntro),
        new(DialogMacroStateIds.FieldGroupSummary, DialogMacroTransitionTriggers.SummaryAccepted, DialogMacroStateIds.ProcessSummary),
        new(DialogMacroStateIds.FieldGroupSummary, DialogMacroTransitionTriggers.SummaryRejected, DialogMacroStateIds.FieldLoop),
        new(DialogMacroStateIds.FieldGroupSummary, DialogMacroTransitionTriggers.CancelConfirmed, DialogMacroStateIds.Cancelled),
        new(DialogMacroStateIds.ProcessSummary, DialogMacroTransitionTriggers.ProcessConfirmed, DialogMacroStateIds.Submitted),
        new(DialogMacroStateIds.ProcessSummary, DialogMacroTransitionTriggers.ProcessRejected, DialogMacroStateIds.FieldGroupIntro),
        new(DialogMacroStateIds.ProcessSummary, DialogMacroTransitionTriggers.CancelConfirmed, DialogMacroStateIds.Cancelled),
    ];

    private static readonly ImmutableArray<FormDialogMicroTransition> MicroTransitions =
    [
        new(DialogMicroStateIds.Prompting, DialogMicroTransitionTriggers.PromptComplete, DialogMicroStateIds.Listening),
        new(DialogMicroStateIds.Listening, DialogMicroTransitionTriggers.SpeechReceived, DialogMicroStateIds.Interpreting),
        new(DialogMicroStateIds.Listening, DialogMicroTransitionTriggers.NoInputTimeout, DialogMicroStateIds.Prompting),
        new(DialogMicroStateIds.Listening, DialogMicroTransitionTriggers.MaxRetriesExceeded, DialogMicroStateIds.ErrorPrompt),
        new(DialogMicroStateIds.Interpreting, DialogMicroTransitionTriggers.InterpretationComplete, DialogMicroStateIds.Validating),
        // Die Ablehnung eines wegen geringer Erkennungssicherheit angebotenen Vorlesens
        // kann noch während der Interpretation eintreffen. Auch dieser Übergang führt
        // in den Reparaturzustand.
        new(DialogMicroStateIds.Interpreting, DialogMicroTransitionTriggers.UserRejected, DialogMicroStateIds.Repair),
        new(DialogMicroStateIds.Validating, DialogMicroTransitionTriggers.ValidationPassed, DialogMicroStateIds.Confirmation),
        new(DialogMicroStateIds.Validating, DialogMicroTransitionTriggers.ValidationFailed, DialogMicroStateIds.ErrorPrompt),
        new(DialogMicroStateIds.Confirmation, DialogMicroTransitionTriggers.UserConfirmed, null),
        new(DialogMicroStateIds.Confirmation, DialogMicroTransitionTriggers.UserRejected, DialogMicroStateIds.Repair),
        new(DialogMicroStateIds.ErrorPrompt, DialogMicroTransitionTriggers.ErrorPromptComplete, DialogMicroStateIds.Listening),
        new(DialogMicroStateIds.Repair, DialogMicroTransitionTriggers.RepairComplete, DialogMicroStateIds.Listening),
    ];

    public static IReadOnlyList<FormDialogMacroTransition> MacroTransitionTable => MacroTransitions;

    public static IReadOnlyList<FormDialogMicroTransition> MicroTransitionTable => MicroTransitions;

    public static string ApplyMacroTransition(string currentState, string trigger, string expectedNextState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentState);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedNextState);

        var normalizedCurrentState = currentState.Trim();
        var normalizedTrigger = trigger.Trim();
        var normalizedExpectedNextState = expectedNextState.Trim();

        foreach (var transition in MacroTransitions)
        {
            if (!string.Equals(transition.FromState, normalizedCurrentState, StringComparison.Ordinal)
                || !string.Equals(transition.Trigger, normalizedTrigger, StringComparison.Ordinal)
                || !string.Equals(transition.ToState, normalizedExpectedNextState, StringComparison.Ordinal))
            {
                continue;
            }

            return transition.ToState;
        }

        throw new InvalidOperationException(
            $"Unsupported dialog macro transition '{normalizedCurrentState}' --{normalizedTrigger}--> '{normalizedExpectedNextState}'.");
    }

    public static string? ApplyMicroTransition(string currentState, string trigger, string? expectedNextState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentState);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        var normalizedCurrentState = currentState.Trim();
        var normalizedTrigger = trigger.Trim();
        var normalizedExpectedNextState = string.IsNullOrWhiteSpace(expectedNextState)
            ? null
            : expectedNextState.Trim();

        foreach (var transition in MicroTransitions)
        {
            if (!string.Equals(transition.FromState, normalizedCurrentState, StringComparison.Ordinal)
                || !string.Equals(transition.Trigger, normalizedTrigger, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(transition.ToState, normalizedExpectedNextState, StringComparison.Ordinal))
            {
                continue;
            }

            return transition.ToState;
        }

        var formattedExpectedNextState = normalizedExpectedNextState ?? "<exit>";
        throw new InvalidOperationException(
            $"Unsupported dialog micro transition '{normalizedCurrentState}' --{normalizedTrigger}--> '{formattedExpectedNextState}'.");
    }
}
