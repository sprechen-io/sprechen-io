namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Funktionen für Abschlussansage und Ergebnisbildung einer Auswahl bereit.
/// Der Abschlussablauf führt sie später aus. Die Definition selbst hat keine
/// Wirkung auf den laufenden Dialog.
/// </summary>
public sealed class SelectionDialogLaunchDefinition<TSelection, TResult>
    where TSelection : class
{
    public SelectionDialogLaunchDefinition(
        Func<SelectionCandidate<TSelection>, SelectionDialogTurnSnapshot<TSelection>, SelectionDialogPrompt> buildPrompt,
        Func<SelectionCandidate<TSelection>, SelectionDialogTurnSnapshot<TSelection>, TResult> projectResult)
    {
        BuildPrompt = buildPrompt ?? throw new ArgumentNullException(nameof(buildPrompt));
        ProjectResult = projectResult ?? throw new ArgumentNullException(nameof(projectResult));
    }

    public Func<SelectionCandidate<TSelection>, SelectionDialogTurnSnapshot<TSelection>, SelectionDialogPrompt> BuildPrompt { get; }

    public Func<SelectionCandidate<TSelection>, SelectionDialogTurnSnapshot<TSelection>, TResult> ProjectResult { get; }
}
