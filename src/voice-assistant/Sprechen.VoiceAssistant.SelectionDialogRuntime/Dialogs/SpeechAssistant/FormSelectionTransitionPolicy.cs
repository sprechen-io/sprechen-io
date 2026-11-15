namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft vor Bestätigung oder Start die noch fehlenden Formularvoraussetzungen. Fehlende
/// Vorgänger führen mit einer Ansage zurück zum Ausgangsbestand. Sonst entscheidet die
/// Bestätigungsvorgabe über den nächsten Schritt.
/// </summary>
internal sealed class FormSelectionTransitionPolicy
{
    private readonly SelectionConfirmationPolicyEvaluator _confirmationPolicyEvaluator;

    public FormSelectionTransitionPolicy(
        SelectionConfirmationPolicyEvaluator confirmationPolicyEvaluator)
    {
        _confirmationPolicyEvaluator = confirmationPolicyEvaluator
            ?? throw new ArgumentNullException(nameof(confirmationPolicyEvaluator));
    }

    public Task<SelectionDialogIntentStep<FormInfo>> EvaluateAsync(
        SelectionCandidate<FormInfo> selected,
        SelectionCandidateResolutionKind resolutionKind,
        FormSelectionTransitionContext transitionContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(transitionContext);
        _ = cancellationToken;

        var missingPrerequisites = GetMissingPrerequisiteNames(selected.Item, transitionContext);
        if (missingPrerequisites.Count > 0)
        {
            var prerequisitesPrompt = FormSelectionPromptBuilder.GetPrerequisitesNotMetPrompt(
                transitionContext.PromptContext,
                selected.Item,
                missingPrerequisites,
                transitionContext.IsoCode,
                transitionContext.PolitenessLevel);
            return Task.FromResult(new SelectionDialogIntentStep<FormInfo>(
                SelectionDialogPhase.Listening,
                SelectionDialogCandidateTransition<FormInfo>.ResetToInitial(),
                prerequisitesPrompt));
        }

        if (_confirmationPolicyEvaluator.RequiresConfirmation(
                transitionContext.ConfirmationPolicy,
                resolutionKind))
        {
            return Task.FromResult(new SelectionDialogIntentStep<FormInfo>(
                SelectionDialogPhase.Confirming,
                SelectionDialogCandidateTransition<FormInfo>.SelectUnique(selected, resolutionKind),
                spokenPrompt: null));
        }

        return Task.FromResult(new SelectionDialogIntentStep<FormInfo>(
            SelectionDialogPhase.Launch,
            SelectionDialogCandidateTransition<FormInfo>.SelectUnique(selected, resolutionKind),
            spokenPrompt: null));
    }

    private static List<string> GetMissingPrerequisiteNames(
        FormInfo selected,
        FormSelectionTransitionContext transitionContext)
        => selected.Prerequisites
            .Where(prerequisite => !transitionContext.CompletedFormIds.Contains(prerequisite))
            .Select(prerequisite =>
                transitionContext.FormNamesById.TryGetValue(prerequisite, out var name) ? name : prerequisite)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
}
