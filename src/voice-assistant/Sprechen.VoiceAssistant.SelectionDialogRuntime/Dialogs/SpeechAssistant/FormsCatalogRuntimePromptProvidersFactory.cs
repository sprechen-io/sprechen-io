namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die Ansagefunktionen einer Katalogsitzung für die gemeinsame Laufzeit bereit. Reguläre
/// Rückfragen und Fehlermeldungen greifen dadurch auf dieselben Sprach- und Höflichkeitsvorgaben
/// zu.
/// </summary>
internal sealed class FormsCatalogRuntimePromptProvidersFactory
{
    public SelectionDialogPromptProviders<ApplicationInfo> Create(
        FormsCatalogDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogPromptProviders<ApplicationInfo>(
            getEmptyItemsPromptAsync: (_, _) => Task.FromResult(
                FormsCatalogPromptBuilder.GetNoApplicationsPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getAnnouncingPromptAsync: (snapshot, _) => Task.FromResult(
                FormsCatalogPromptBuilder.GetAnnouncingPrompt(
                    session.PromptContext,
                    snapshot.VisibleCandidates.Candidates,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getNoInputPromptAsync: (_, _) => Task.FromResult(
                FormsCatalogPromptBuilder.GetNoInputPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getReListenPromptAsync: (_, _) => Task.FromResult(
                FormsCatalogPromptBuilder.GetReListenPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getCaptureFailedTerminalPromptAsync: (_, _) => Task.FromResult(
                FormsCatalogPromptBuilder.GetCaptureFailedTerminalPrompt(
                    session.PromptContext,
                    session.IsoCode,
                    session.PolitenessLevel)),
            getRecoverableFailurePromptAsync: (_, _) => Task.FromResult(
                SelectionDialogPrompt.FromText(
                    RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt(session.IsoCode, session.PolitenessLevel))),
            getConfirmationPromptAsync: (request, _) => Task.FromResult(
                FormsCatalogPromptBuilder.ResolveConfirmationPrompt(
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
