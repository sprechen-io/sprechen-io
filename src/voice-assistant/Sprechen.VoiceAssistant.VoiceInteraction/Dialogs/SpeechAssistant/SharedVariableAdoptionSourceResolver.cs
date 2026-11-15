using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht den ersten nicht leeren Wert in den vorgegebenen abgeschlossenen Quellformularen, damit die Übernahme die festgelegte Quellreihenfolge wahrt.
/// </summary>
internal sealed class SharedVariableAdoptionSourceResolver
{
    public bool TryResolve(
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableArray<string> sourceFormIds,
        string variableId,
        out SharedVariableAdoptionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        foreach (var sourceFormId in sourceFormIds)
        {
            if (!TryResolveFromForm(applicationSession, sourceFormId, variableId, out candidate))
            {
                continue;
            }

            return true;
        }

        candidate = null!;
        return false;
    }

    private static bool TryResolveFromForm(
        SpeechAssistantApplicationSessionState applicationSession,
        string sourceFormId,
        string variableId,
        out SharedVariableAdoptionCandidate candidate)
    {
        if (applicationSession.FormsById.TryGetValue(sourceFormId, out var sourceSession)
            && sourceSession.IsSubmitted
            && sourceSession.ValuesByVariableId.TryGetValue(variableId, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            candidate = new SharedVariableAdoptionCandidate(variableId, value, sourceFormId);
            return true;
        }

        candidate = null!;
        return false;
    }
}
