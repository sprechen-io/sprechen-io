namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt der gemeinsamen Laufzeit die Ansagefunktionen der Formularsitzung bereit. Sprach- und
/// Höflichkeitsvorgaben werden dadurch für reguläre und fehlerbedingte Rückfragen gemeinsam
/// verwendet.
/// </summary>
internal sealed class FormSelectionRuntimePromptProvidersFactory
{
    public SelectionDialogPromptProviders<FormInfo> Create(FormSelectionDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogPromptProviders<FormInfo>(
            getEmptyItemsPromptAsync: (_, _) => Task.FromResult(
                FormSelectionPromptBuilder.GetNoFormsPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getAnnouncingPromptAsync: (snapshot, _) => Task.FromResult(
                FormSelectionPromptBuilder.GetAnnouncingPrompt(
                    session.PromptContext,
                    snapshot.VisibleCandidates.Candidates,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getNoInputPromptAsync: (_, _) => Task.FromResult(
                FormSelectionPromptBuilder.GetNoInputPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getReListenPromptAsync: (_, _) => Task.FromResult(
                FormSelectionPromptBuilder.GetReListenPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getCaptureFailedTerminalPromptAsync: (_, _) => Task.FromResult(
                FormSelectionPromptBuilder.GetCaptureFailedTerminalPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getRecoverableFailurePromptAsync: (_, _) => Task.FromResult(
                SelectionDialogPrompt.FromText(
                    RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt(session.IsoCode, session.PolitenessLevel))),
            getConfirmationPromptAsync: (request, _) => Task.FromResult(
                FormSelectionPromptBuilder.ResolveConfirmationPrompt(
                    session.PromptContext,
                    request,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getRecognizerUnavailablePromptAsync: (_, _) => Task.FromResult(
                SelectionDialogPrompt.FromText(
                    RecoverableCapturePromptTextResolver.GetRecognizerUnavailablePrompt(
                        session.IsoCode,
                        session.PolitenessLevel))));
    }
}
