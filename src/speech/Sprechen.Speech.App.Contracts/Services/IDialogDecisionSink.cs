namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Empfängt strukturierte Dialogentscheidungen zur Aufzeichnung.</summary>
/// <remarks>
/// Die Implementierung bestimmt Ziel und Darstellung. Fehler der Aufzeichnung
/// dürfen den aufrufenden Dialog nicht unterbrechen.
/// </remarks>
public interface IDialogDecisionSink
{
    ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken);
}
