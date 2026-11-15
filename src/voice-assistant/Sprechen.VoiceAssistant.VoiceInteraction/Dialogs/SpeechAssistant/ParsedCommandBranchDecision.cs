namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält den gewählten Zweig der Befehlsbehandlung und gegebenenfalls den Fehlercode zusammen.
/// </summary>
internal sealed record ParsedCommandBranchDecision(
    ParsedCommandBranchKind Kind,
    string IssueCode)
{
    public static ParsedCommandBranchDecision None()
        => new(ParsedCommandBranchKind.None, string.Empty);

    public static ParsedCommandBranchDecision PromptedFailure(string issueCode)
        => new(ParsedCommandBranchKind.PromptedFailure, issueCode);

    public static ParsedCommandBranchDecision ManualCorrection()
        => new(ParsedCommandBranchKind.ManualCorrection, string.Empty);

    public static ParsedCommandBranchDecision Execution()
        => new(ParsedCommandBranchKind.Execution, string.Empty);
}
