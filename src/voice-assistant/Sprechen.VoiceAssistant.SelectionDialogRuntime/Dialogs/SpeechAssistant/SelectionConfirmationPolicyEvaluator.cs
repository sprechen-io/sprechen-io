using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet die Bestätigungspflicht aus der Auswahlvorgabe und der Art des Kandidatentreffers ab.
/// Teilnamen und Kennungstreffer bleiben auch bei sonst abgeschalteter Bestätigung
/// rückfragepflichtig.
/// </summary>
internal sealed class SelectionConfirmationPolicyEvaluator
{
    public bool RequiresConfirmation(
        ConfirmationPolicy confirmationPolicy,
        SelectionCandidateResolutionKind resolutionKind)
    {
        if (!Enum.IsDefined(resolutionKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(resolutionKind),
                resolutionKind,
                "Unknown candidate resolution kind.");
        }

        return RequiresConfirmation(
            confirmationPolicy,
            resolutionKind is SelectionCandidateResolutionKind.PartialName
                or SelectionCandidateResolutionKind.Identifier);
    }

    public bool RequiresConfirmation(ConfirmationPolicy confirmationPolicy, bool isAmbiguous)
    {
        if (isAmbiguous)
        {
            return true;
        }

        return confirmationPolicy switch
        {
            ConfirmationPolicy.Always => true,
            ConfirmationPolicy.Ambiguous => false,
            ConfirmationPolicy.Never => false,
            _ => true
        };
    }
}
