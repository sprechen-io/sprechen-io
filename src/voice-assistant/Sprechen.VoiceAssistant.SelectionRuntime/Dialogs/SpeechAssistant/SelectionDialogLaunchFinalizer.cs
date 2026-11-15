namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Abschlussansage, Abbruchprüfung und Ergebnisbildung für einen ausgewählten
/// Kandidaten. Der Aufrufer erhält das Ergebnis erst nach dem vorgesehenen Ansageaufruf.
/// </summary>
internal sealed class SelectionDialogLaunchFinalizer<TSelection, TResult>
    where TSelection : class
{
    private readonly SelectionDialogExecutionSettings<TSelection> _settings;
    private readonly SelectionDialogEvidenceTrace<TSelection> _evidenceTrace;

    public SelectionDialogLaunchFinalizer(
        SelectionDialogExecutionSettings<TSelection> settings,
        SelectionDialogEvidenceTrace<TSelection> evidenceTrace)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _evidenceTrace = evidenceTrace ?? throw new ArgumentNullException(nameof(evidenceTrace));
    }

    public async Task<TResult> FinalizeAsync(
        SelectionCandidate<TSelection> candidate,
        SelectionDialogTurnSnapshot<TSelection> turnSnapshot,
        SelectionDialogLaunchDefinition<TSelection, TResult> launchDefinition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(turnSnapshot);
        ArgumentNullException.ThrowIfNull(launchDefinition);

        var launchPrompt = launchDefinition.BuildPrompt(candidate, turnSnapshot)
            ?? throw new SelectionCandidateStateException(
                "A selection launch definition must produce a prompt.");
        await _evidenceTrace
            .LaunchPromptScheduledAsync(candidate, launchPrompt, cancellationToken)
            .ConfigureAwait(false);
        await _settings.Speech
            .SpeakAsync(
                launchPrompt,
                _settings.IsoCode,
                _settings.InteractionSettings,
                cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        var result = launchDefinition.ProjectResult(candidate, turnSnapshot);
        await _evidenceTrace
            .SelectionLaunchedAsync(candidate, cancellationToken)
            .ConfigureAwait(false);
        return result;
    }
}
