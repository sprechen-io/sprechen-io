namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält offene Rückfragen zur Sprache und zur Anwendungsbeschreibung sowie die Sprache
/// für einen angeforderten Neustart fest. Diese Abläufe bleiben von der generischen Kandidatenauswahl getrennt.
/// </summary>
internal sealed class FormsCatalogDialogRuntimeState
{
    public bool AwaitingLanguageTarget { get; private set; }

    public bool AwaitingDescriptionCandidate { get; private set; }

    public string? RestartIsoCode { get; private set; }

    public void BeginDescriptionResolution()
    {
        AwaitingDescriptionCandidate = true;
    }

    public void ClearDescriptionResolution()
    {
        AwaitingDescriptionCandidate = false;
    }

    public void Apply(FormsCatalogIntentHandlingResult intentHandling)
    {
        ArgumentNullException.ThrowIfNull(intentHandling);
        AwaitingLanguageTarget = intentHandling.ExpectsLanguageTarget;
        RestartIsoCode = intentHandling.RestartIsoCode;

        if (intentHandling.NextPhase is SelectionDialogPhase.Completed or SelectionDialogPhase.Launch)
        {
            ClearDescriptionResolution();
        }
    }
}
