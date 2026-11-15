namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet Anwendungsvergleich, Katalogansagen und Bestätigungsvorgaben an die gemeinsame Intent-
/// Behandlung. Allgemeine Auswahlbefehle bleiben dadurch für Anwendungs- und Formularauswahl
/// gemeinsam implementiert.
/// </summary>
internal sealed class FormsCatalogRuntimeIntentHandlerFactory
{
    private readonly FormsCatalogSelectionTransitionPolicy _transitionPolicy;
    private readonly ApplicationMatcher _applicationMatcher;

    public FormsCatalogRuntimeIntentHandlerFactory(
        FormsCatalogSelectionTransitionPolicy transitionPolicy,
        ApplicationMatcher applicationMatcher)
    {
        _transitionPolicy = transitionPolicy ?? throw new ArgumentNullException(nameof(transitionPolicy));
        _applicationMatcher = applicationMatcher ?? throw new ArgumentNullException(nameof(applicationMatcher));
    }

    public SelectionDialogCommonIntentHandler<ApplicationInfo> Create(FormsCatalogDialogSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SelectionDialogCommonIntentHandler<ApplicationInfo>(
            matcher: _applicationMatcher.CandidateMatcher,
            buildListPrompt: candidates => FormsCatalogPromptBuilder.GetListPrompt(
                session.PromptContext,
                candidates,
                session.IsoCode,
                session.PolitenessLevel),
            buildAmbiguityPrompt: candidates => FormsCatalogPromptBuilder.GetAmbiguityPrompt(
                session.PromptContext,
                candidates,
                session.IsoCode,
                session.PolitenessLevel),
            buildCancelPrompt: () => FormsCatalogPromptBuilder.GetCancelPrompt(
                session.PromptContext,
                session.IsoCode,
                session.PolitenessLevel),
            buildHelpPrompt: () => FormsCatalogPromptBuilder.GetHelpPrompt(
                session.PromptContext,
                session.IsoCode,
                session.PolitenessLevel),
            buildInvalidNumberPrompt: max => FormsCatalogPromptBuilder.GetInvalidNumberPrompt(
                session.PromptContext,
                max,
                session.IsoCode,
                session.PolitenessLevel),
            buildNoMatchPrompt: name => FormsCatalogPromptBuilder.GetNoMatchPrompt(
                session.PromptContext,
                name,
                session.IsoCode,
                session.PolitenessLevel),
            transitionAfterSelectionAsync: (selected, resolutionKind, ct) =>
                _transitionPolicy.EvaluateAsync(
                    selected,
                    resolutionKind,
                    session.ConfirmationPolicy,
                    session.PromptContext,
                    session.PolitenessLevel,
                    session.IsoCode,
                    ct));
    }

}
