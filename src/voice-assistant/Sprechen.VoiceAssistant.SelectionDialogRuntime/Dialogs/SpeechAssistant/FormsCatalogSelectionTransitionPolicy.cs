using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt anhand von Auflösungsart und Bestätigungsvorgabe den nächsten Schritt einer
/// eindeutigen Anwendungsauswahl. Die generische Laufzeit erhält dadurch einen Bestätigungs- oder
/// Startvorschlag samt Kandidat.
/// </summary>
internal sealed class FormsCatalogSelectionTransitionPolicy
{
    private readonly SelectionConfirmationPolicyEvaluator _confirmationPolicyEvaluator;

    public FormsCatalogSelectionTransitionPolicy(
        SelectionConfirmationPolicyEvaluator confirmationPolicyEvaluator)
    {
        _confirmationPolicyEvaluator = confirmationPolicyEvaluator
            ?? throw new ArgumentNullException(nameof(confirmationPolicyEvaluator));
    }

    public Task<SelectionDialogIntentStep<ApplicationInfo>> EvaluateAsync(
        SelectionCandidate<ApplicationInfo> selected,
        SelectionCandidateResolutionKind resolutionKind,
        ConfirmationPolicy confirmationPolicy,
        CatalogPromptContext promptContext,
        PolitenessLevel politenessLevel,
        string isoCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        _ = cancellationToken;

        if (_confirmationPolicyEvaluator.RequiresConfirmation(
                confirmationPolicy,
                resolutionKind))
        {
            return Task.FromResult(new SelectionDialogIntentStep<ApplicationInfo>(
                SelectionDialogPhase.Confirming,
                SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
                spokenPrompt: null));
        }

        return Task.FromResult(new SelectionDialogIntentStep<ApplicationInfo>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<ApplicationInfo>.SelectUnique(selected, resolutionKind),
            spokenPrompt: null));
    }
}
