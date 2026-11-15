namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Leere Diagnosesenke, die eine optionale Aufzeichnung ohne Nullprüfungen ermöglicht.
/// Sie prüft weiterhin Eingabe und Abbruchtoken.
/// </summary>
public sealed class NullDialogDecisionSink : IDialogDecisionSink
{
    public static readonly NullDialogDecisionSink Instance = new();

    private NullDialogDecisionSink()
    {
    }

    public ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}
