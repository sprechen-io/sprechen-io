using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt Dialogentscheidungen für Prüfungen ihrer benannten Felder.
/// Die Prüfungen hängen dadurch nicht vom Wortlaut einer formatierten Ausgabe ab.
/// </summary>
internal sealed class RecordingDialogDecisionSink : IDialogDecisionSink
{
    private readonly List<DialogDecision> _decisions = [];

    public ImmutableArray<DialogDecision> Decisions => [.. _decisions];

    public ValueTask RecordAsync(DialogDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        cancellationToken.ThrowIfCancellationRequested();
        _decisions.Add(decision);
        return ValueTask.CompletedTask;
    }

    public DialogDecision Single(string name)
        => _decisions.Single(decision => string.Equals(decision.Name, name, StringComparison.Ordinal));

    public static string Field(DialogDecision decision, string fieldName)
        => decision.Fields.Single(field => string.Equals(field.Name, fieldName, StringComparison.Ordinal)).Value;
}
